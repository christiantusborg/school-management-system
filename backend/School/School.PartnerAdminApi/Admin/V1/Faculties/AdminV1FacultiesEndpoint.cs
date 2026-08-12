using System.Text.Json;
using Odin.Api.Base.Authorization;
using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Faculties;

/// <summary>
/// The Faculties feature — its OWN tables, not datasheets. System Config
/// edits THE global Faculty Profile Information structure; per partner the
/// Admission Office manages TEACHERS (partner users with the Teacher role)
/// whose profiles are shaped by that structure.
/// </summary>
[Route("/v1/admin/faculty-profile-structure")]
[EndpointTag("Admin.Faculties")]
public sealed class AdminV1FacultiesEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/faculty-profile-structure", GetStructureAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/faculty-profile-structure", SaveStructureAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partners/{partnerId:guid}/teachers", ListTeachersAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/teachers", CreateTeacherAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/teachers/{teacherId:guid}", GetProfileAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/teachers/{teacherId:guid}", SaveProfileAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/teachers/{teacherId:guid}", DeleteTeacherAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/faculty-files", UploadFileAsync).RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapGet("/v1/admin/faculty-values/{valueId:guid}/file", DownloadFileAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/faculty-values/{valueId:guid}/file/{index:int}", DownloadFilesItemAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class FieldDto
    {
        public Guid? Id { get; init; }
        public string? Label { get; init; }
        public string? Type { get; init; }
        public string? OptionsText { get; init; }
        public string? Tooltip { get; init; }
        public bool IsRequired { get; init; }
        public bool PartnerCanEdit { get; init; }
    }

    public sealed class SectionDto
    {
        public Guid? Id { get; init; }
        public string? Title { get; init; }
        public string? Kind { get; init; }
        public List<FieldDto>? Fields { get; init; }
    }

    public sealed class StructureBody
    {
        public List<SectionDto>? Sections { get; init; }
    }

    public sealed class CreateTeacherBody
    {
        public string? TeacherUserId { get; init; }
        public string? DisplayName { get; init; }
    }

    private static async Task<IResult> GetStructureAsync(OdinDbContext db, CancellationToken ct) =>
        Results.Ok(new { sections = await FacultyProfileLogic.StructureAsync(db, ct) });

    /// <summary>
    /// Wholesale structure save with soft-delete semantics: sections/fields
    /// missing from the payload are hidden (values survive); a new field
    /// whose label matches a soft-deleted one restores it — and its data.
    /// </summary>
    private static async Task<IResult> SaveStructureAsync(
        [FromBody] StructureBody body, HttpContext http, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "config.faculty_structure", ct) != AccessLevel.Edit) return Results.Forbid();

        var incoming = body.Sections ?? [];
        foreach (var s in incoming)
        {
            if (string.IsNullOrWhiteSpace(s.Title))
                return Results.BadRequest(new { error = "Every section needs a title." });
            if (!FacultyProfileLogic.Kinds.Contains(s.Kind))
                return Results.BadRequest(new { error = $"Unknown section kind '{s.Kind}'." });
            foreach (var f in s.Fields ?? [])
            {
                if (string.IsNullOrWhiteSpace(f.Label))
                    return Results.BadRequest(new { error = $"Every field in \"{s.Title}\" needs a label." });
                if (!FacultyProfileLogic.FieldTypes.Contains(f.Type))
                    return Results.BadRequest(new { error = $"Unknown field type '{f.Type}'." });
            }
        }

        var allSections = await db.FacultyProfileSections.ToListAsync(ct);
        var allFields = await db.FacultyProfileFields.ToListAsync(ct);

        var keptSectionIds = new HashSet<Guid>();
        var sectionOrder = 0;
        foreach (var s in incoming)
        {
            var section = s.Id is { } sid ? allSections.FirstOrDefault(x => x.FacultyProfileSectionId == sid) : null;
            if (section is null)
            {
                section = new FacultyProfileSection();
                db.FacultyProfileSections.Add(section);
                allSections.Add(section);
            }
            section.Title = s.Title!.Trim();
            section.Kind = s.Kind!;
            section.SortOrder = sectionOrder++;
            section.DeletedAt = null;
            keptSectionIds.Add(section.FacultyProfileSectionId);

            var sectionFields = allFields.Where(f => f.FacultyProfileSectionId == section.FacultyProfileSectionId).ToList();
            var keptFieldIds = new HashSet<Guid>();
            var fieldOrder = 0;
            foreach (var f in s.Fields ?? [])
            {
                var field = f.Id is { } fid ? sectionFields.FirstOrDefault(x => x.FacultyProfileFieldId == fid) : null;
                field ??= sectionFields.FirstOrDefault(x =>
                    x.DeletedAt != null && string.Equals(x.Label, f.Label!.Trim(), StringComparison.OrdinalIgnoreCase));
                if (field is null)
                {
                    field = new FacultyProfileField { FacultyProfileSectionId = section.FacultyProfileSectionId };
                    db.FacultyProfileFields.Add(field);
                    allFields.Add(field);
                    sectionFields.Add(field);
                }
                field.Label = f.Label!.Trim();
                field.Type = f.Type!;
                field.OptionsText = f.Type == FacultyProfileField.TypeSelect || FacultyProfileLogic.IsSystemType(f.Type)
                    ? f.OptionsText
                    : null;
                field.Tooltip = string.IsNullOrWhiteSpace(f.Tooltip) ? null : f.Tooltip.Trim();
                field.IsRequired = f.IsRequired;
                field.PartnerCanEdit = f.PartnerCanEdit && !FacultyProfileLogic.IsSystemType(f.Type);
                field.SortOrder = fieldOrder++;
                field.DeletedAt = null;
                keptFieldIds.Add(field.FacultyProfileFieldId);
            }
            foreach (var f in sectionFields.Where(x => x.DeletedAt == null && !keptFieldIds.Contains(x.FacultyProfileFieldId)))
                f.DeletedAt = DateTime.UtcNow;
        }
        foreach (var s in allSections.Where(x => x.DeletedAt == null && !keptSectionIds.Contains(x.FacultyProfileSectionId)))
            s.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> ListTeachersAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var partnerExists = await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (!partnerExists) return Results.NotFound();

        var items = await db.Teachers
            .Where(t => t.PartnerId == partnerId && t.DeletedAt == null)
            .OrderBy(t => t.DisplayName)
            .Select(t => new
            {
                teacherId = t.TeacherId,
                displayName = t.DisplayName,
                userId = t.UserId,
                username = db.Users.Where(u => u.Id == t.UserId).Select(u => u.UserName).FirstOrDefault(),
                email = db.Users.Where(u => u.Id == t.UserId).Select(u => u.Email).FirstOrDefault(),
                isEnabled = db.Users.Where(u => u.Id == t.UserId).Select(u => (bool?)u.IsEnabled).FirstOrDefault(),
                updatedAt = t.UpdatedAt ?? t.CreatedAt,
            })
            .ToListAsync(ct);
        return Results.Ok(new { items });
    }

    /// <summary>
    /// Registers a teacher. The login user (partner user with IsTeacher) is
    /// created first via the existing partner-users endpoint; this binds the
    /// Teacher record to it. TeacherUserId may be null only for profile-only
    /// records.
    /// </summary>
    private static async Task<IResult> CreateTeacherAsync(
        Guid partnerId, [FromBody] CreateTeacherBody body, OdinDbContext db,
        HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.faculty.add", ct) != AccessLevel.Edit) return Results.Forbid();

        var partnerExists = await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (!partnerExists) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(body.DisplayName))
            return Results.BadRequest(new { error = "Teacher name is required." });

        string? userId = null;
        if (!string.IsNullOrWhiteSpace(body.TeacherUserId))
        {
            var user = await db.Users
                .Where(u => u.Id == body.TeacherUserId && u.PartnerId == partnerId && u.DeletedAt == null)
                .Select(u => new { u.Id, u.IsTeacher })
                .FirstOrDefaultAsync(ct);
            if (user is null) return Results.BadRequest(new { error = "Teacher user not found on this partner." });
            if (!user.IsTeacher) return Results.BadRequest(new { error = "That user is not a Teacher." });
            var taken = await db.Teachers.AnyAsync(t => t.UserId == user.Id && t.DeletedAt == null, ct);
            if (taken) return Results.Conflict(new { error = "That user already has a teacher record." });
            userId = user.Id;
        }

        var teacher = new Teacher
        {
            PartnerId = partnerId,
            UserId = userId,
            DisplayName = body.DisplayName.Trim(),
        };
        db.Teachers.Add(teacher);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { teacherId = teacher.TeacherId });
    }

    private static async Task<IResult> GetProfileAsync(Guid teacherId, OdinDbContext db, CancellationToken ct)
    {
        var profile = await FacultyProfileLogic.ProfileAsync(db, teacherId, ct);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> SaveProfileAsync(
        Guid teacherId, [FromBody] FacultyProfileLogic.SaveBody body, OdinDbContext db,
        HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.faculty.edit", ct) != AccessLevel.Edit) return Results.Forbid();

        var ok = await FacultyProfileLogic.SaveProfileAsync(db, teacherId, body, partnerEditableOnly: false, ct);
        return ok ? Results.Ok(new { saved = true }) : Results.NotFound();
    }

    private static async Task<IResult> DeleteTeacherAsync(
        Guid teacherId, OdinDbContext db, HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.faculty.delete", ct) != AccessLevel.Edit) return Results.Forbid();

        var teacher = await db.Teachers
            .FirstOrDefaultAsync(t => t.TeacherId == teacherId && t.DeletedAt == null, ct);
        if (teacher is null) return Results.NotFound();
        teacher.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> UploadFileAsync(
        IFormFile file, IFileStorage storage, HttpContext http, IPermissionService perms, CancellationToken ct)
    {
        if (await perms.AccessAsync(http.User, "partner.faculty.edit", ct) != AccessLevel.Edit) return Results.Forbid();

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "file is required" });
        if (file.Length > 50 * 1024 * 1024)
            return Results.BadRequest(new { error = "Max file size is 50 MB." });

        var safeName = Path.GetFileName(file.FileName);
        string storagePath;
        await using (var stream = file.OpenReadStream())
        {
            storagePath = await storage.SaveAsync(stream, $"{FacultyProfileLogic.StoragePrefix}{Guid.NewGuid():N}-{safeName}", ct);
        }
        return Results.Ok(new { token = storagePath, fileName = safeName });
    }

    private static async Task<IResult> DownloadFileAsync(
        Guid valueId, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var value = await db.TeacherProfileValues
            .Where(v => v.TeacherProfileValueId == valueId)
            .Select(v => new { v.Value, v.FileName })
            .FirstOrDefaultAsync(ct);
        if (value is null || !value.Value.Contains(FacultyProfileLogic.StoragePrefix)) return Results.NotFound();

        try
        {
            var stream = await storage.OpenReadAsync(value.Value, ct);
            return Results.File(stream, "application/octet-stream", value.FileName ?? "faculty-file");
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }

    /// <summary>
    /// Streams one file of a "files" cell. The cell's Value is a JSON array of
    /// {token,name}; the token at {index} is served exactly like the single
    /// "file" download (must contain the storage prefix, minted by upload).
    /// </summary>
    private static async Task<IResult> DownloadFilesItemAsync(
        Guid valueId, int index, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var value = await db.TeacherProfileValues
            .Where(v => v.TeacherProfileValueId == valueId)
            .Select(v => new { v.Value })
            .FirstOrDefaultAsync(ct);
        if (value is null) return Results.NotFound();

        var item = FacultyProfileLogic.FilesItemAt(value.Value, index);
        if (item is null || !item.Token.Contains(FacultyProfileLogic.StoragePrefix)) return Results.NotFound();

        try
        {
            var stream = await storage.OpenReadAsync(item.Token, ct);
            return Results.File(stream, "application/octet-stream", item.Name ?? "faculty-file");
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
