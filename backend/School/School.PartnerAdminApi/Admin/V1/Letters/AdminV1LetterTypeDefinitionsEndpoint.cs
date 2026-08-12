using System.Security.Claims;
using Odin.Api.Base.Authorization;

namespace School.PartnerAdminApi.Admin.V1.Letters;

/// <summary>
/// System Config CRUD for config-created letter types and the letter
/// language list. Types run alongside the built-in LetterType enum letters
/// (which stay untouched until they migrate into this table). Reading is
/// open to all admin staff (the Letters surfaces need the list); creating,
/// editing and deleting are SuperAdministrator-only, like the changelog.
/// </summary>
[Route("/v1/admin/letter-type-definitions")]
[EndpointTag("Admin.Letters")]
public sealed class AdminV1LetterTypeDefinitionsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/letter-type-definitions", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/letter-type-definitions", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/letter-type-definitions/{id:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/letter-type-definitions/{id:guid}", DeleteAsync).RequireAuthorization("AdminOnly");

        app.MapGet("/v1/admin/letter-languages", LanguagesListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/letter-languages", LanguageCreateAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/letter-languages/{id:guid}", LanguageUpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/letter-languages/{id:guid}", LanguageDeleteAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class TypeBody
    {
        public string? Name { get; init; }
        public string? ReferencePrefix { get; init; }
        public Guid? TriggerStatusId { get; init; }
        public bool VisibleToStudent { get; init; }
        public bool VisibleToPartner { get; init; }
        public bool EmailOnRelease { get; init; }
        public bool AllowLegacyUpload { get; init; }
        public int SortOrder { get; init; }
    }

    public sealed class LanguageBody
    {
        public string? Name { get; init; }
        public int SortOrder { get; init; }
        /// <summary>Alias used by the shared SimpleListManager frontend.</summary>
        public int DisplayOrder { get; init; }
    }

    private static int EffectiveOrder(LanguageBody body) =>
        body.SortOrder != 0 ? body.SortOrder : body.DisplayOrder;

    private static async Task<IResult?> RequireSuperAdminAsync(
        HttpContext http, UserManager<ApplicationUser> userManager,
        IPermissionService perms, CancellationToken ct)
    {
        var callerId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();
        var caller = await userManager.FindByIdAsync(callerId);
        if (caller is null || caller.DeletedAt is not null || !caller.IsEnabled)
            return Results.Unauthorized();
        if (!await perms.HasAsync(http.User, AdminPermissions.LetterTypesManage, ct))
            return Results.Json(new { error = "Letter type configuration requires SuperAdministrator." },
                statusCode: StatusCodes.Status403Forbidden);
        return null;
    }

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.LetterTypeDefinitions
            .Where(d => d.DeletedAt == null)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new
            {
                letterTypeDefinitionId = d.LetterTypeDefinitionId,
                name = d.Name,
                referencePrefix = d.ReferencePrefix,
                documentTypeId = d.DocumentTypeId,
                triggerStatusId = d.TriggerStatusId,
                triggerStatusName = d.TriggerStatusId == null
                    ? null
                    : db.EnrollmentStatuses.Where(s => s.EnrollmentStatusId == d.TriggerStatusId)
                        .Select(s => s.Name).FirstOrDefault(),
                visibleToStudent = d.VisibleToStudent,
                visibleToPartner = d.VisibleToPartner,
                emailOnRelease = d.EmailOnRelease,
                allowLegacyUpload = d.AllowLegacyUpload,
                sortOrder = d.SortOrder,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static string NormalizePrefix(string? prefix, string? name)
    {
        var p = (prefix ?? string.Empty).Trim().ToUpperInvariant();
        p = new string(p.Where(char.IsLetterOrDigit).ToArray());
        if (p.Length > 0) return p;
        // Auto-suggest from the name's initials (e.g. "Drop-out Letter" → "DL").
        var initials = (name ?? string.Empty)
            .Split(' ', '-', '(', ')', '/')
            .Where(w => w.Length > 0 && char.IsLetter(w[0]))
            .Select(w => char.ToUpperInvariant(w[0]));
        p = new string(initials.Take(3).ToArray());
        return p.Length > 0 ? p : "LT";
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] TypeBody body, HttpContext http,
        UserManager<ApplicationUser> userManager, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await RequireSuperAdminAsync(http, userManager, perms, ct) is { } fail) return fail;
        var name = (body.Name ?? string.Empty).Trim();
        if (name.Length == 0) return Results.BadRequest(new { error = "Name is required." });
        if (await db.LetterTypeDefinitions.AnyAsync(d => d.DeletedAt == null && d.Name == name, ct))
            return Results.BadRequest(new { error = "A letter type with that name already exists." });

        var docType = new DocumentType
        {
            DocumentTypeId = Guid.NewGuid(),
            Name = name,
            Description = "System-generated letter PDF (config-created letter type).",
            IsSystemGenerated = true,
        };
        db.DocumentTypes.Add(docType);

        var def = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.LetterTypeDefinition
        {
            Name = name,
            ReferencePrefix = NormalizePrefix(body.ReferencePrefix, name),
            DocumentTypeId = docType.DocumentTypeId,
            TriggerStatusId = body.TriggerStatusId,
            VisibleToStudent = body.VisibleToStudent,
            VisibleToPartner = body.VisibleToPartner,
            EmailOnRelease = body.EmailOnRelease,
            AllowLegacyUpload = body.AllowLegacyUpload,
            SortOrder = body.SortOrder,
        };
        db.LetterTypeDefinitions.Add(def);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { letterTypeDefinitionId = def.LetterTypeDefinitionId });
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, [FromBody] TypeBody body, HttpContext http,
        UserManager<ApplicationUser> userManager, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await RequireSuperAdminAsync(http, userManager, perms, ct) is { } fail) return fail;
        var def = await db.LetterTypeDefinitions
            .FirstOrDefaultAsync(d => d.LetterTypeDefinitionId == id && d.DeletedAt == null, ct);
        if (def is null) return Results.NotFound();
        var name = (body.Name ?? string.Empty).Trim();
        if (name.Length == 0) return Results.BadRequest(new { error = "Name is required." });

        def.Name = name;
        def.ReferencePrefix = NormalizePrefix(body.ReferencePrefix, name);
        def.TriggerStatusId = body.TriggerStatusId;
        def.VisibleToStudent = body.VisibleToStudent;
        def.VisibleToPartner = body.VisibleToPartner;
        def.EmailOnRelease = body.EmailOnRelease;
        def.AllowLegacyUpload = body.AllowLegacyUpload;
        def.SortOrder = body.SortOrder;

        var docType = await db.DocumentTypes.FirstOrDefaultAsync(t => t.DocumentTypeId == def.DocumentTypeId, ct);
        if (docType is not null) docType.Name = name;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { updated = true });
    }

    private static async Task<IResult> DeleteAsync(
        Guid id, HttpContext http,
        UserManager<ApplicationUser> userManager, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await RequireSuperAdminAsync(http, userManager, perms, ct) is { } fail) return fail;
        var def = await db.LetterTypeDefinitions
            .FirstOrDefaultAsync(d => d.LetterTypeDefinitionId == id && d.DeletedAt == null, ct);
        if (def is null) return Results.NotFound();
        // Soft delete: released documents and their versions stay downloadable.
        def.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> LanguagesListAsync(OdinDbContext db, CancellationToken ct)
    {
        var items = await db.LetterLanguages
            .Where(l => l.DeletedAt == null)
            .OrderBy(l => l.SortOrder).ThenBy(l => l.Name)
            .Select(l => new { letterLanguageId = l.LetterLanguageId, name = l.Name, sortOrder = l.SortOrder, displayOrder = l.SortOrder })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    private static async Task<IResult> LanguageCreateAsync(
        [FromBody] LanguageBody body, HttpContext http,
        UserManager<ApplicationUser> userManager, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await RequireSuperAdminAsync(http, userManager, perms, ct) is { } fail) return fail;
        var name = (body.Name ?? string.Empty).Trim();
        if (name.Length == 0) return Results.BadRequest(new { error = "Name is required." });
        if (string.Equals(name, "English", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "English is the built-in default language." });
        if (await db.LetterLanguages.AnyAsync(l => l.DeletedAt == null && l.Name == name, ct))
            return Results.BadRequest(new { error = "That language is already on the list." });
        var lang = new SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes.LetterLanguage
        {
            Name = name,
            SortOrder = EffectiveOrder(body),
        };
        db.LetterLanguages.Add(lang);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { letterLanguageId = lang.LetterLanguageId });
    }

    private static async Task<IResult> LanguageUpdateAsync(
        Guid id, [FromBody] LanguageBody body, HttpContext http,
        UserManager<ApplicationUser> userManager, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await RequireSuperAdminAsync(http, userManager, perms, ct) is { } fail) return fail;
        var lang = await db.LetterLanguages
            .FirstOrDefaultAsync(l => l.LetterLanguageId == id && l.DeletedAt == null, ct);
        if (lang is null) return Results.NotFound();
        var name = (body.Name ?? string.Empty).Trim();
        if (name.Length == 0) return Results.BadRequest(new { error = "Name is required." });
        lang.Name = name;
        lang.SortOrder = EffectiveOrder(body);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { updated = true });
    }

    private static async Task<IResult> LanguageDeleteAsync(
        Guid id, HttpContext http,
        UserManager<ApplicationUser> userManager, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await RequireSuperAdminAsync(http, userManager, perms, ct) is { } fail) return fail;
        var lang = await db.LetterLanguages
            .FirstOrDefaultAsync(l => l.LetterLanguageId == id && l.DeletedAt == null, ct);
        if (lang is null) return Results.NotFound();
        lang.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }
}
