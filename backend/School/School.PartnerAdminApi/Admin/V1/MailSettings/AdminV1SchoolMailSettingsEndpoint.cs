using System.Security.Claims;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Email;
using SharedLibrary.Basics.Opaque.Domains;

namespace School.PartnerAdminApi.Admin.V1.MailConfig;

/// <summary>
/// Per-school outbound-mail configuration edited in System Config → Schools →
/// Edit. "Default" means the school uses the global settings (System Config →
/// Email); "Gmail"/"Smtp" give the school its own transport, with automatic
/// fallback to the global one when a send fails. Secrets are write-only, like
/// the global endpoint. Restricted to Administrator and SuperAdministrator.
/// </summary>
[Route("/v1/admin/schools/{schoolId:guid}/mail-settings")]
[EndpointTag("Admin.MailSettings")]
public sealed class AdminV1SchoolMailSettingsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/schools/{schoolId:guid}/mail-settings", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/schools/{schoolId:guid}/mail-settings", SaveAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/schools/{schoolId:guid}/mail-settings/test", TestAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class WriteRequest
    {
        public string? Provider { get; init; }
        public string? GmailImpersonatedUser { get; init; }
        public string? FromEmail { get; init; }
        public string? FromName { get; init; }
        /// <summary>New service-account JSON. Omit/blank to keep the existing one.</summary>
        public string? GmailServiceAccountJson { get; init; }

        public string? SmtpHost { get; init; }
        public int? SmtpPort { get; init; }
        public string? SmtpUsername { get; init; }
        public string? SmtpSecurity { get; init; }
        /// <summary>New SMTP password. Omit/blank to keep the existing one.</summary>
        public string? SmtpPassword { get; init; }
    }

    public sealed class TestRequest
    {
        public string? To { get; init; }
    }

    private static async Task<bool> IsAdministratorAsync(
        HttpContext http, UserManager<ApplicationUser> userManager,
        IPermissionService perms, CancellationToken ct)
    {
        var callerId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return false;
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled) return false;
        return await perms.HasAsync(http.User, AdminPermissions.MailSettings, ct);
    }

    private static async Task<IResult> GetAsync(
        Guid schoolId, HttpContext http, [FromServices] UserManager<ApplicationUser> userManager, [FromServices] IPermissionService perms,
        OdinDbContext db, CancellationToken ct)
    {
        if (!await IsAdministratorAsync(http, userManager, perms, ct))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);
        if (!await db.Schools.AnyAsync(s => s.SchoolId == schoolId && s.DeletedAt == null, ct))
            return Results.NotFound();

        var s = await db.SchoolMailSettings.FirstOrDefaultAsync(x => x.SchoolId == schoolId, ct);
        return Results.Ok(new
        {
            provider = s?.Provider ?? "Default",
            gmailImpersonatedUser = s?.GmailImpersonatedUser,
            fromEmail = s?.FromEmail,
            fromName = s?.FromName,
            hasServiceAccount = !string.IsNullOrWhiteSpace(s?.GmailServiceAccountJson),
            smtpHost = s?.SmtpHost,
            smtpPort = s?.SmtpPort,
            smtpUsername = s?.SmtpUsername,
            smtpSecurity = s?.SmtpSecurity,
            hasSmtpPassword = !string.IsNullOrWhiteSpace(s?.SmtpPassword),
            updatedAt = s?.UpdatedAt,
        });
    }

    private static async Task<IResult> SaveAsync(
        Guid schoolId, HttpContext http, [FromServices] UserManager<ApplicationUser> userManager, [FromServices] IPermissionService perms,
        [FromBody] WriteRequest body, OdinDbContext db, CancellationToken ct)
    {
        if (!await IsAdministratorAsync(http, userManager, perms, ct))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);
        if (!await db.Schools.AnyAsync(s => s.SchoolId == schoolId && s.DeletedAt == null, ct))
            return Results.NotFound();

        var provider = body.Provider ?? "Default";
        if (provider is not ("Default" or "Gmail" or "Smtp"))
            return Results.BadRequest(new { error = "provider must be 'Default', 'Gmail' or 'Smtp'." });

        var s = await db.SchoolMailSettings.FirstOrDefaultAsync(x => x.SchoolId == schoolId, ct);
        if (s is null)
        {
            s = new SchoolMailSettings { SchoolId = schoolId };
            db.SchoolMailSettings.Add(s);
        }
        s.Provider = provider;
        s.GmailImpersonatedUser = body.GmailImpersonatedUser;
        s.FromEmail = body.FromEmail;
        s.FromName = body.FromName;
        s.SmtpHost = body.SmtpHost;
        s.SmtpPort = body.SmtpPort;
        s.SmtpUsername = body.SmtpUsername;
        s.SmtpSecurity = body.SmtpSecurity;
        // Only replace secrets when a new value is supplied.
        if (!string.IsNullOrWhiteSpace(body.GmailServiceAccountJson))
            s.GmailServiceAccountJson = body.GmailServiceAccountJson;
        if (!string.IsNullOrWhiteSpace(body.SmtpPassword))
            s.SmtpPassword = body.SmtpPassword;
        s.UpdatedAt = DateTime.UtcNow;
        s.UpdatedByUserId = Guid.TryParse(http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : null;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            saved = true,
            hasServiceAccount = !string.IsNullOrWhiteSpace(s.GmailServiceAccountJson),
            hasSmtpPassword = !string.IsNullOrWhiteSpace(s.SmtpPassword),
        });
    }

    private static async Task<IResult> TestAsync(
        Guid schoolId, HttpContext http, [FromServices] UserManager<ApplicationUser> userManager, [FromServices] IPermissionService perms,
        [FromBody] TestRequest body, OdinDbContext db,
        [FromServices] ISchoolMailTester tester, CancellationToken ct)
    {
        if (!await IsAdministratorAsync(http, userManager, perms, ct))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);
        if (!await db.Schools.AnyAsync(s => s.SchoolId == schoolId && s.DeletedAt == null, ct))
            return Results.NotFound();
        if (string.IsNullOrWhiteSpace(body.To))
            return Results.BadRequest(new { error = "A recipient address is required." });

        // Strictly the school's own transport — no fallback — so a green test
        // proves this school's configuration, not the default one.
        try
        {
            await tester.SendTestAsync(schoolId, body.To.Trim(), ct);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { sent = false, error = ex.Message });
        }
        return Results.Ok(new { sent = true, to = body.To.Trim() });
    }
}
