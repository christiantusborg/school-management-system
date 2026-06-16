using System.Security.Claims;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Email;
using SharedLibrary.Basics.Opaque.Domains;

namespace School.PartnerAdminApi.Admin.V1.MailConfig;

/// <summary>
/// Outbound-mail configuration edited in System Config → Email. Holds the
/// Gmail service-account key (stored encrypted at rest), the impersonated
/// Workspace user, and the From identity. The service-account JSON is never
/// returned to the client; the GET only reports whether one is present.
/// Restricted to Administrator and SuperAdministrator.
/// </summary>
[Route("/v1/admin/mail-settings")]
[EndpointTag("Admin.MailSettings")]
public sealed class AdminV1MailSettingsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/mail-settings", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/mail-settings", SaveAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/mail-settings/test", TestAsync).RequireAuthorization("AdminOnly");
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
        HttpContext http, UserManager<ApplicationUser> userManager)
    {
        var callerId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(callerId)) return false;
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled) return false;
        return await userManager.IsInRoleAsync(caller, AdminLevels.Administrator)
            || await userManager.IsInRoleAsync(caller, AdminLevels.SuperAdministrator);
    }

    private static async Task<MailSettings> LoadOrCreateAsync(OdinDbContext db, CancellationToken ct)
    {
        var settings = await db.MailSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            settings = new MailSettings { MailSettingsId = MailSettings.SingletonId };
            db.MailSettings.Add(settings);
        }
        return settings;
    }

    private static async Task<IResult> GetAsync(
        HttpContext http, [FromServices] UserManager<ApplicationUser> userManager,
        OdinDbContext db, CancellationToken ct)
    {
        if (!await IsAdministratorAsync(http, userManager))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var s = await db.MailSettings.FirstOrDefaultAsync(ct);
        return Results.Ok(new
        {
            provider = s?.Provider ?? "Brevo",
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
        HttpContext http, [FromServices] UserManager<ApplicationUser> userManager,
        [FromBody] WriteRequest body, OdinDbContext db, CancellationToken ct)
    {
        if (!await IsAdministratorAsync(http, userManager))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);

        var provider = body.Provider ?? "Brevo";
        if (provider is not ("Brevo" or "Gmail" or "Smtp"))
            return Results.BadRequest(new { error = "provider must be 'Brevo', 'Gmail' or 'Smtp'." });

        var s = await LoadOrCreateAsync(db, ct);
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

        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            saved = true,
            hasServiceAccount = !string.IsNullOrWhiteSpace(s.GmailServiceAccountJson),
            hasSmtpPassword = !string.IsNullOrWhiteSpace(s.SmtpPassword),
        });
    }

    private static async Task<IResult> TestAsync(
        HttpContext http, [FromServices] UserManager<ApplicationUser> userManager,
        [FromBody] TestRequest body, IEmailSender emailSender, CancellationToken ct)
    {
        if (!await IsAdministratorAsync(http, userManager))
            return Results.Json(new { error = "Requires Administrator level or above." }, statusCode: StatusCodes.Status403Forbidden);
        if (string.IsNullOrWhiteSpace(body.To))
            return Results.BadRequest(new { error = "A recipient address is required." });

        try
        {
            await emailSender.SendAsync(new EmailMessage(
                To: body.To.Trim(),
                Subject: "IBSS mail configuration test",
                HtmlBody: "<p>This is a test email from the IBSS admin portal. If you received it, outbound mail is configured correctly.</p>"),
                ct);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { sent = false, error = ex.Message });
        }
        return Results.Ok(new { sent = true, to = body.To.Trim() });
    }
}
