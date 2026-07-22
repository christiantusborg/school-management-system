using System.Security.Claims;
using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.ModuleCohorts;

/// <summary>
/// MGW-admin side of the Module Cohort Schedule: System Config (number
/// pattern + configurable upload fields), per-partner cohort CRUD, student
/// assignment (both from the cohort and per student), multi-file uploads,
/// QA fields and the global overview with the missing-QA reports.
/// </summary>
[Route("/v1/admin/cohorts")]
[EndpointTag("Admin.ModuleCohorts")]
public sealed class AdminV1ModuleCohortsEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/cohort-settings", GetSettingsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/cohort-types", ListTypesAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/cohort-types", CreateTypeAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/cohort-types/{typeId:guid}", RenameTypeAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/cohort-types/{typeId:guid}/structure", SaveTypeStructureAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/cohort-types/{typeId:guid}", DeleteTypeAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/cohort-types/{typeId:guid}/clone", CloneTypeAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/cohort-field-files", UploadFieldFileAsync)
            .RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapGet("/v1/admin/cohort-field-values/{valueId:guid}/file", DownloadFieldFileAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/cohort-settings", SaveSettingsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partners/{partnerId:guid}/cohorts", ListAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partners/{partnerId:guid}/cohort-sources", SourcesAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/cohorts", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/cohorts/{cohortId:guid}", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/cohorts/{cohortId:guid}", UpdateAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/cohorts/{cohortId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/cohorts/{cohortId:guid}/students", StudentsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/cohorts/{cohortId:guid}/grades", GradesAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/cohorts/{cohortId:guid}/grades/draft", SaveGradesDraftAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/cohorts/{cohortId:guid}/grades/submit", SubmitGradesAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/cohorts/{cohortId:guid}/students", AssignStudentsAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/cohorts/{cohortId:guid}/files", UploadFilesAsync)
            .RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapDelete("/v1/admin/cohort-files/{fileId:guid}", DeleteFileAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/cohort-files/{fileId:guid}/file", DownloadFileAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/cohorts-overview", OverviewAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/cohorts", StudentCohortsAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/students/{studentId:guid}/enrollments/{enrollmentId:guid}/cohorts", SetStudentCohortAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class SettingsFieldDto
    {
        public Guid? Id { get; init; }
        public string? Label { get; init; }
        public bool AllowMultiple { get; init; }
        public bool IsGradingSheet { get; init; }
        public bool VisibleToStudents { get; init; }
    }

    public sealed class SettingsBody
    {
        public string? CohortNumberPattern { get; init; }
        public List<SettingsFieldDto>? Fields { get; init; }
    }

    public sealed class CreateBody
    {
        public Guid? ProgrammeId { get; init; }
        public Guid? SubjectId { get; init; }
        public Guid? TeacherId { get; init; }
        public Guid? CohortTypeId { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
    }

    public sealed class FieldValueDto
    {
        public string? Value { get; init; }
        public string? FileName { get; init; }
    }

    public sealed class UpdateBody
    {
        public Guid? CohortTypeId { get; init; }
        public Dictionary<string, FieldValueDto>? FieldValues { get; init; }
        public Guid? TeacherId { get; init; }
        public DateTime? StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public DateTime? GradingSheetDueOverride { get; init; }
        public bool ClearDueOverride { get; init; }
        public DateTime? GradingSheetUploadedDate { get; init; }
        public bool? DocQaChecked { get; init; }
        public DateTime? DocQaDate { get; init; }
        public bool? GradeQaChecked { get; init; }
        public DateTime? GradeQaDate { get; init; }
    }

    public sealed class AssignBody
    {
        public List<Guid>? EnrollmentIds { get; init; }
    }

    public sealed class SetStudentCohortBody
    {
        public Guid SubjectId { get; init; }
        public Guid? CohortId { get; init; }
    }

    private static DateTime? Norm(DateTime? d) =>
        d is { } x ? DateTime.SpecifyKind(x.Date, DateTimeKind.Unspecified) : null;

    private static async Task<IResult> GetSettingsAsync(OdinDbContext db, CancellationToken ct)
    {
        var settings = await ModuleCohortLogic.SettingsAsync(db, ct);
        var fields = await db.CohortUploadFields
            .Where(f => f.DeletedAt == null)
            .OrderBy(f => f.SortOrder)
            .Select(f => new
            {
                id = f.CohortUploadFieldId,
                label = f.Label,
                allowMultiple = f.AllowMultiple,
                isGradingSheet = f.IsGradingSheet,
                visibleToStudents = f.VisibleToStudents,
            })
            .ToListAsync(ct);
        return Results.Ok(new { cohortNumberPattern = settings.CohortNumberPattern, fields });
    }

    private static async Task<IResult> SaveSettingsAsync(
        [FromBody] SettingsBody body, OdinDbContext db, CancellationToken ct)
    {
        var settings = await ModuleCohortLogic.SettingsAsync(db, ct);
        if (!string.IsNullOrWhiteSpace(body.CohortNumberPattern))
            settings.CohortNumberPattern = body.CohortNumberPattern.Trim();
        // Upload fields moved into the cohort types — settings carry only
        // the number pattern now.
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> ListAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();
        return Results.Ok(new { items = await ModuleCohortLogic.ListAsync(db, partnerId, null, ct) });
    }

    /// <summary>Dropdown sources: the partner's programmes (granted core +
    /// own custom), each programme's modules, and the partner's teachers.</summary>
    private static async Task<IResult> SourcesAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var grantedIds = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && pp.IsActive != null)
            .Select(pp => pp.ProgrammeId)
            .ToListAsync(ct);
        var programmes = await db.Programmes
            .Where(p => p.DeletedAt == null && (grantedIds.Contains(p.ProgrammeId) || p.OwnerId == partnerId))
            .OrderBy(p => p.Name)
            .Select(p => new { programmeId = p.ProgrammeId, code = p.Code, name = p.Name })
            .ToListAsync(ct);
        var programmeIds = programmes.Select(p => p.programmeId).ToList();

        var modules = await (
            from s in db.Subjects
            join sp in db.Specializations on s.SpecializationId equals sp.SpecializationId
            where s.DeletedAt == null && programmeIds.Contains(sp.ProgrammeId)
            orderby s.Code
            select new
            {
                subjectId = s.SubjectId,
                programmeId = sp.ProgrammeId,
                specializationName = sp.Name,
                code = s.Code,
                name = s.Name,
            }).ToListAsync(ct);

        var teachers = await db.Teachers
            .Where(t => t.PartnerId == partnerId && t.DeletedAt == null)
            .OrderBy(t => t.DisplayName)
            .Select(t => new { teacherId = t.TeacherId, displayName = t.DisplayName })
            .ToListAsync(ct);
        var cohortTypes = await db.CohortTypes
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => new { cohortTypeId = t.CohortTypeId, name = t.Name })
            .ToListAsync(ct);

        return Results.Ok(new { programmes, modules, teachers, cohortTypes });
    }

    private static async Task<IResult> CreateAsync(
        Guid partnerId, [FromBody] CreateBody body, OdinDbContext db, CancellationToken ct)
    {
        if (!await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct))
            return Results.NotFound();
        if (body.ProgrammeId is null || body.SubjectId is null)
            return Results.BadRequest(new { error = "Programme and module are required." });
        if (body.CohortTypeId is null
            || !await db.CohortTypes.AnyAsync(t => t.CohortTypeId == body.CohortTypeId && t.DeletedAt == null, ct))
            return Results.BadRequest(new { error = "A cohort type is required." });
        var moduleOk = await (
            from s in db.Subjects
            join sp in db.Specializations on s.SpecializationId equals sp.SpecializationId
            where s.SubjectId == body.SubjectId && sp.ProgrammeId == body.ProgrammeId && s.DeletedAt == null
            select s.SubjectId).AnyAsync(ct);
        if (!moduleOk) return Results.BadRequest(new { error = "That module does not belong to the chosen programme." });
        if (body.TeacherId is not null)
        {
            var teacherOk = await db.Teachers.AnyAsync(t =>
                t.TeacherId == body.TeacherId && t.PartnerId == partnerId && t.DeletedAt == null, ct);
            if (!teacherOk) return Results.BadRequest(new { error = "Unknown teacher for this partner." });
        }

        var cohort = new ModuleCohort
        {
            PartnerId = partnerId,
            ProgrammeId = body.ProgrammeId.Value,
            SubjectId = body.SubjectId.Value,
            CohortTypeId = body.CohortTypeId,
            TeacherId = body.TeacherId,
            StartDate = Norm(body.StartDate),
            EndDate = Norm(body.EndDate),
            CohortNumber = await ModuleCohortLogic.NextCohortNumberAsync(db, partnerId, body.SubjectId.Value, ct),
        };
        db.ModuleCohorts.Add(cohort);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { moduleCohortId = cohort.ModuleCohortId, cohortNumber = cohort.CohortNumber });
    }

    private static async Task<IResult> GetAsync(Guid cohortId, OdinDbContext db, CancellationToken ct)
    {
        var detail = await ModuleCohortLogic.DetailAsync(db, cohortId, ct);
        return detail is null ? Results.NotFound() : Results.Ok(detail);
    }

    private static async Task<IResult> UpdateAsync(
        Guid cohortId, [FromBody] UpdateBody body, OdinDbContext db, CancellationToken ct)
    {
        var cohort = await db.ModuleCohorts
            .FirstOrDefaultAsync(c => c.ModuleCohortId == cohortId && c.DeletedAt == null, ct);
        if (cohort is null) return Results.NotFound();

        if (body.TeacherId is not null)
        {
            var teacherOk = await db.Teachers.AnyAsync(t =>
                t.TeacherId == body.TeacherId && t.PartnerId == cohort.PartnerId && t.DeletedAt == null, ct);
            if (!teacherOk) return Results.BadRequest(new { error = "Unknown teacher for this partner." });
        }
        cohort.TeacherId = body.TeacherId;
        cohort.StartDate = Norm(body.StartDate);
        var oldEnd = cohort.EndDate;
        cohort.EndDate = Norm(body.EndDate);
        // Reset reminder stages when the schedule moves.
        if (oldEnd != cohort.EndDate)
        {
            cohort.Reminder2WeeksSent = false;
            cohort.Reminder1WeekSent = false;
            cohort.ReminderOverdueSent = false;
        }
        cohort.GradingSheetDueOverride = body.ClearDueOverride ? null : (Norm(body.GradingSheetDueOverride) ?? cohort.GradingSheetDueOverride);
        cohort.GradingSheetUploadedDate = Norm(body.GradingSheetUploadedDate);
        if (body.DocQaChecked is { } dq) cohort.DocQaChecked = dq;
        cohort.DocQaDate = Norm(body.DocQaDate);
        if (body.GradeQaChecked is { } gq) cohort.GradeQaChecked = gq;
        cohort.GradeQaDate = Norm(body.GradeQaDate);
        if (body.CohortTypeId is { } newType)
        {
            var typeOk = await db.CohortTypes.AnyAsync(t => t.CohortTypeId == newType && t.DeletedAt == null, ct);
            if (!typeOk) return Results.BadRequest(new { error = "Unknown cohort type." });
            cohort.CohortTypeId = newType;
        }
        await ModuleCohortLogic.SaveFieldValuesAsync(db, cohort.ModuleCohortId,
            body.FieldValues?.ToDictionary(kv => kv.Key, kv => (kv.Value?.Value, kv.Value?.FileName)), ct);
        cohort.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> DeleteAsync(Guid cohortId, OdinDbContext db, CancellationToken ct)
    {
        var cohort = await db.ModuleCohorts
            .FirstOrDefaultAsync(c => c.ModuleCohortId == cohortId && c.DeletedAt == null, ct);
        if (cohort is null) return Results.NotFound();
        cohort.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static readonly string[] TypeFieldTypes =
    [
        CohortTypeField.TypeText, CohortTypeField.TypeNumber, CohortTypeField.TypeDate,
        CohortTypeField.TypeSelect, CohortTypeField.TypeBool, CohortTypeField.TypeFile,
        CohortTypeField.TypeInfo,
    ];

    public sealed class TypeFieldDto
    {
        public Guid? Id { get; init; }
        public string? Label { get; init; }
        public string? Type { get; init; }
        public string? OptionsText { get; init; }
        public bool IsRequired { get; init; }
    }

    public sealed class TypeUploadFieldDto
    {
        public Guid? Id { get; init; }
        public string? Label { get; init; }
        public bool AllowMultiple { get; init; }
        public bool IsGradingSheet { get; init; }
        public bool VisibleToStudents { get; init; }
    }

    public sealed class TypeStructureBody
    {
        public List<TypeFieldDto>? Fields { get; init; }
        public List<TypeUploadFieldDto>? UploadFields { get; init; }
    }

    private static async Task<IResult> ListTypesAsync(OdinDbContext db, CancellationToken ct)
    {
        var types = await db.CohortTypes
            .Where(t => t.DeletedAt == null)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .Select(t => new
            {
                t.CohortTypeId,
                t.Name,
                InUse = db.ModuleCohorts.Count(c => c.CohortTypeId == t.CohortTypeId && c.DeletedAt == null),
            })
            .ToListAsync(ct);
        var typeIds = types.Select(t => t.CohortTypeId).ToList();
        var fields = await db.CohortTypeFields
            .Where(f => typeIds.Contains(f.CohortTypeId) && f.DeletedAt == null)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);
        var uploadFields = await db.CohortUploadFields
            .Where(f => f.CohortTypeId != null && typeIds.Contains(f.CohortTypeId.Value) && f.DeletedAt == null)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);
        return Results.Ok(new
        {
            items = types.Select(t => new
            {
                cohortTypeId = t.CohortTypeId,
                name = t.Name,
                inUse = t.InUse,
                locked = t.InUse > 0,
                fields = fields.Where(f => f.CohortTypeId == t.CohortTypeId).Select(f => new
                {
                    id = f.CohortTypeFieldId,
                    label = f.Label,
                    type = f.Type,
                    optionsText = f.OptionsText,
                    isRequired = f.IsRequired,
                }).ToList(),
                uploadFields = uploadFields.Where(f => f.CohortTypeId == t.CohortTypeId).Select(f => new
                {
                    id = f.CohortUploadFieldId,
                    label = f.Label,
                    allowMultiple = f.AllowMultiple,
                    isGradingSheet = f.IsGradingSheet,
                    visibleToStudents = f.VisibleToStudents,
                }).ToList(),
            }).ToList(),
        });
    }

    private static async Task<IResult> CreateTypeAsync(
        [FromBody] Dictionary<string, string?> body, OdinDbContext db, CancellationToken ct)
    {
        var name = body.GetValueOrDefault("name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });
        var type = new CohortType { Name = name };
        db.CohortTypes.Add(type);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { cohortTypeId = type.CohortTypeId });
    }

    private static async Task<IResult> RenameTypeAsync(
        Guid typeId, [FromBody] Dictionary<string, string?> body, OdinDbContext db, CancellationToken ct)
    {
        var type = await db.CohortTypes.FirstOrDefaultAsync(t => t.CohortTypeId == typeId && t.DeletedAt == null, ct);
        if (type is null) return Results.NotFound();
        if (await db.ModuleCohorts.AnyAsync(c => c.CohortTypeId == typeId && c.DeletedAt == null, ct))
            return Results.BadRequest(new { error = "This cohort type is assigned to a cohort and is locked — clone it to make changes." });
        var name = body.GetValueOrDefault("name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });
        type.Name = name;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { cohortTypeId = typeId });
    }

    /// <summary>Wholesale field save with soft-delete + restore-by-label so
    /// values entered on cohorts survive structure edits.</summary>
    private static async Task<IResult> SaveTypeStructureAsync(
        Guid typeId, [FromBody] TypeStructureBody body, OdinDbContext db, CancellationToken ct)
    {
        var type = await db.CohortTypes.FirstOrDefaultAsync(t => t.CohortTypeId == typeId && t.DeletedAt == null, ct);
        if (type is null) return Results.NotFound();
        if (await db.ModuleCohorts.AnyAsync(c => c.CohortTypeId == typeId && c.DeletedAt == null, ct))
            return Results.BadRequest(new { error = "This cohort type is assigned to a cohort and is locked — clone it to make changes." });
        foreach (var f in body.Fields ?? [])
        {
            if (string.IsNullOrWhiteSpace(f.Label))
                return Results.BadRequest(new { error = "Every field needs a label." });
            if (!TypeFieldTypes.Contains(f.Type))
                return Results.BadRequest(new { error = $"Unknown field type '{f.Type}'." });
        }
        var all = await db.CohortTypeFields.Where(f => f.CohortTypeId == typeId).ToListAsync(ct);
        var kept = new HashSet<Guid>();
        var order = 0;
        foreach (var f in body.Fields ?? [])
        {
            var field = f.Id is { } fid ? all.FirstOrDefault(x => x.CohortTypeFieldId == fid) : null;
            field ??= all.FirstOrDefault(x =>
                x.DeletedAt != null && string.Equals(x.Label, f.Label!.Trim(), StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                field = new CohortTypeField { CohortTypeId = typeId };
                db.CohortTypeFields.Add(field);
                all.Add(field);
            }
            field.Label = f.Label!.Trim();
            field.Type = f.Type!;
            field.OptionsText = f.Type is CohortTypeField.TypeSelect or CohortTypeField.TypeInfo ? f.OptionsText : null;
            field.IsRequired = f.IsRequired;
            field.SortOrder = order++;
            field.DeletedAt = null;
            kept.Add(field.CohortTypeFieldId);
        }
        foreach (var f in all.Where(x => x.DeletedAt == null && !kept.Contains(x.CohortTypeFieldId)))
            f.DeletedAt = DateTime.UtcNow;

        // Upload fields of this type — same soft-delete/restore-by-label rules.
        var allUploads = await db.CohortUploadFields.Where(f => f.CohortTypeId == typeId).ToListAsync(ct);
        var keptUploads = new HashSet<Guid>();
        var uOrder = 0;
        foreach (var f in body.UploadFields ?? [])
        {
            if (string.IsNullOrWhiteSpace(f.Label)) continue;
            var field = f.Id is { } uid ? allUploads.FirstOrDefault(x => x.CohortUploadFieldId == uid) : null;
            field ??= allUploads.FirstOrDefault(x =>
                x.DeletedAt != null && string.Equals(x.Label, f.Label.Trim(), StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                field = new CohortUploadField { CohortTypeId = typeId };
                db.CohortUploadFields.Add(field);
                allUploads.Add(field);
            }
            field.Label = f.Label.Trim();
            field.AllowMultiple = f.AllowMultiple;
            field.IsGradingSheet = f.IsGradingSheet;
            field.VisibleToStudents = f.VisibleToStudents;
            field.SortOrder = uOrder++;
            field.DeletedAt = null;
            keptUploads.Add(field.CohortUploadFieldId);
        }
        foreach (var f in allUploads.Where(x => x.DeletedAt == null && !keptUploads.Contains(x.CohortUploadFieldId)))
            f.DeletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    /// <summary>Full editable copy of a (typically locked) type: name +
    /// " (copy)", all data fields and upload fields.</summary>
    private static async Task<IResult> CloneTypeAsync(Guid typeId, OdinDbContext db, CancellationToken ct)
    {
        var src = await db.CohortTypes.FirstOrDefaultAsync(t => t.CohortTypeId == typeId && t.DeletedAt == null, ct);
        if (src is null) return Results.NotFound();
        var clone = new CohortType { Name = src.Name + " (copy)" };
        db.CohortTypes.Add(clone);
        var fields = await db.CohortTypeFields
            .Where(f => f.CohortTypeId == typeId && f.DeletedAt == null)
            .OrderBy(f => f.SortOrder).ToListAsync(ct);
        foreach (var f in fields)
            db.CohortTypeFields.Add(new CohortTypeField
            {
                CohortTypeId = clone.CohortTypeId,
                Label = f.Label, Type = f.Type, OptionsText = f.OptionsText,
                IsRequired = f.IsRequired, SortOrder = f.SortOrder,
            });
        var uploads = await db.CohortUploadFields
            .Where(f => f.CohortTypeId == typeId && f.DeletedAt == null)
            .OrderBy(f => f.SortOrder).ToListAsync(ct);
        foreach (var f in uploads)
            db.CohortUploadFields.Add(new CohortUploadField
            {
                CohortTypeId = clone.CohortTypeId,
                Label = f.Label, AllowMultiple = f.AllowMultiple,
                IsGradingSheet = f.IsGradingSheet, VisibleToStudents = f.VisibleToStudents,
                SortOrder = f.SortOrder,
            });
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { cohortTypeId = clone.CohortTypeId });
    }

    private static async Task<IResult> DeleteTypeAsync(Guid typeId, OdinDbContext db, CancellationToken ct)
    {
        var type = await db.CohortTypes.FirstOrDefaultAsync(t => t.CohortTypeId == typeId && t.DeletedAt == null, ct);
        if (type is null) return Results.NotFound();
        var inUse = await db.ModuleCohorts.CountAsync(c => c.CohortTypeId == typeId && c.DeletedAt == null, ct);
        if (inUse > 0)
            return Results.Conflict(new { error = $"This cohort type is used by {inUse} cohort(s)." });
        type.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> UploadFieldFileAsync(
        IFormFile file, IFileStorage storage, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return Results.BadRequest(new { error = "file is required" });
        if (file.Length > 100 * 1024 * 1024) return Results.BadRequest(new { error = "Max file size is 100 MB." });
        var safeName = Path.GetFileName(file.FileName);
        string storagePath;
        await using (var stream = file.OpenReadStream())
        {
            storagePath = await storage.SaveAsync(stream, $"{ModuleCohortLogic.StoragePrefix}fields/{Guid.NewGuid():N}-{safeName}", ct);
        }
        return Results.Ok(new { token = storagePath, fileName = safeName });
    }

    private static async Task<IResult> DownloadFieldFileAsync(
        Guid valueId, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var value = await db.CohortFieldValues
            .Where(v => v.CohortFieldValueId == valueId)
            .Select(v => new { v.Value, v.FileName })
            .FirstOrDefaultAsync(ct);
        if (value is null || !value.Value.Contains(ModuleCohortLogic.StoragePrefix)) return Results.NotFound();
        try
        {
            var stream = await storage.OpenReadAsync(value.Value, ct);
            return Results.File(stream, "application/octet-stream", value.FileName ?? "cohort-file");
        }
        catch (FileNotFoundException) { return Results.NotFound(); }
    }

    private static async Task<IResult> GradesAsync(Guid cohortId, OdinDbContext db, CancellationToken ct)
    {
        var result = await ModuleCohortLogic.GradesAsync(db, cohortId, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> SaveGradesDraftAsync(
        Guid cohortId, [FromBody] ModuleCohortLogic.GradesDraftBody body, OdinDbContext db, CancellationToken ct)
    {
        var (found, error, saved, skipped) = await ModuleCohortLogic.SaveGradesDraftAsync(db, cohortId, body, ct);
        if (!found) return Results.NotFound();
        return error is null ? Results.Ok(new { saved, skipped }) : Results.BadRequest(new { error });
    }

    private static async Task<IResult> SubmitGradesAsync(
        Guid cohortId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        Guid.TryParse(callerId, out var byUserId);
        var result = await ModuleCohortLogic.SubmitGradesAsync(db, cohortId, byUserId, "Admission Office", ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    /// <summary>Assigned students + the partner's assignable (admitted/active)
    /// students enrolled in the cohort's programme.</summary>
    private static async Task<IResult> StudentsAsync(Guid cohortId, OdinDbContext db, CancellationToken ct)
    {
        var cohort = await db.ModuleCohorts
            .Where(c => c.ModuleCohortId == cohortId && c.DeletedAt == null)
            .Select(c => new { c.PartnerId, c.ProgrammeId })
            .FirstOrDefaultAsync(ct);
        if (cohort is null) return Results.NotFound();

        var assignedIds = (await db.ModuleCohortStudents
            .Where(s => s.ModuleCohortId == cohortId && s.DeletedAt == null)
            .Select(s => s.StudentEnrollmentId)
            .ToListAsync(ct)).ToHashSet();

        var candidates = await db.Enrollments
            .Where(e => e.DeletedAt == null && e.PartnerId == cohort.PartnerId
                && e.Specialization.ProgrammeId == cohort.ProgrammeId
                && ModuleCohortLogic.AssignableStatusCodes.Contains(e.Status.Code))
            .Select(e => new
            {
                enrollmentId = e.StudentEnrollmentId,
                studentId = e.StudentId,
                statusName = e.Status.Name,
                studentNumber = db.Students.Where(s => s.StudentId == e.StudentId).Select(s => s.StudentNumber).FirstOrDefault(),
                firstName = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(p => p.UserId == s.UserId).Select(p => p.FirstName).FirstOrDefault()).FirstOrDefault(),
                lastName = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(p => p.UserId == s.UserId).Select(p => p.LastName).FirstOrDefault()).FirstOrDefault(),
            })
            .ToListAsync(ct);

        return Results.Ok(new
        {
            students = candidates.Select(c => new
            {
                c.enrollmentId,
                c.studentId,
                c.studentNumber,
                c.firstName,
                c.lastName,
                c.statusName,
                assigned = assignedIds.Contains(c.enrollmentId),
            }).OrderBy(c => c.lastName).ThenBy(c => c.firstName).ToList(),
        });
    }

    private static async Task<IResult> AssignStudentsAsync(
        Guid cohortId, [FromBody] AssignBody body, OdinDbContext db, CancellationToken ct)
    {
        var cohort = await db.ModuleCohorts
            .FirstOrDefaultAsync(c => c.ModuleCohortId == cohortId && c.DeletedAt == null, ct);
        if (cohort is null) return Results.NotFound();

        var wanted = (body.EnrollmentIds ?? []).ToHashSet();
        var existing = await db.ModuleCohortStudents
            .Where(s => s.ModuleCohortId == cohortId)
            .ToListAsync(ct);
        foreach (var row in existing)
            row.DeletedAt = wanted.Contains(row.StudentEnrollmentId) ? null : (row.DeletedAt ?? DateTime.UtcNow);
        var have = existing.Select(r => r.StudentEnrollmentId).ToHashSet();
        foreach (var id in wanted.Where(id => !have.Contains(id)))
            db.ModuleCohortStudents.Add(new ModuleCohortStudent { ModuleCohortId = cohortId, StudentEnrollmentId = id });

        cohort.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> UploadFilesAsync(
        Guid cohortId, [FromQuery] Guid fieldId, IFormFileCollection files,
        OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var cohort = await db.ModuleCohorts
            .FirstOrDefaultAsync(c => c.ModuleCohortId == cohortId && c.DeletedAt == null, ct);
        if (cohort is null) return Results.NotFound();
        var field = await db.CohortUploadFields
            .FirstOrDefaultAsync(f => f.CohortUploadFieldId == fieldId && f.DeletedAt == null, ct);
        if (field is null) return Results.BadRequest(new { error = "Unknown upload field." });
        if (field.CohortTypeId != null && field.CohortTypeId != cohort.CohortTypeId)
            return Results.BadRequest(new { error = "That upload field belongs to a different cohort type." });
        if (files is null || files.Count == 0)
            return Results.BadRequest(new { error = "No files supplied." });
        if (!field.AllowMultiple && files.Count > 1)
            return Results.BadRequest(new { error = $"\"{field.Label}\" accepts a single document." });
        if (files.Any(f => f.Length > 100 * 1024 * 1024))
            return Results.BadRequest(new { error = "Max file size is 100 MB." });

        // Single-document fields replace the existing file.
        if (!field.AllowMultiple)
        {
            var olds = await db.CohortUploadFiles
                .Where(x => x.ModuleCohortId == cohortId && x.CohortUploadFieldId == fieldId && x.DeletedAt == null)
                .ToListAsync(ct);
            foreach (var old in olds) old.DeletedAt = DateTime.UtcNow;
        }

        foreach (var file in files)
        {
            var safeName = Path.GetFileName(file.FileName);
            string storagePath;
            await using (var stream = file.OpenReadStream())
            {
                storagePath = await storage.SaveAsync(
                    stream, $"{ModuleCohortLogic.StoragePrefix}{cohortId:N}/{Guid.NewGuid():N}-{safeName}", ct);
            }
            db.CohortUploadFiles.Add(new CohortUploadFile
            {
                ModuleCohortId = cohortId,
                CohortUploadFieldId = fieldId,
                FileName = safeName,
                StoragePath = storagePath,
            });
        }

        // Uploading on a grading-sheet field stamps the uploaded date.
        if (field.IsGradingSheet && cohort.GradingSheetUploadedDate is null)
            cohort.GradingSheetUploadedDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);

        cohort.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { uploaded = files.Count });
    }

    private static async Task<IResult> DeleteFileAsync(Guid fileId, OdinDbContext db, CancellationToken ct)
    {
        var file = await db.CohortUploadFiles
            .FirstOrDefaultAsync(f => f.CohortUploadFileId == fileId && f.DeletedAt == null, ct);
        if (file is null) return Results.NotFound();
        file.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    private static async Task<IResult> DownloadFileAsync(
        Guid fileId, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var file = await db.CohortUploadFiles
            .Where(f => f.CohortUploadFileId == fileId && f.DeletedAt == null)
            .Select(f => new { f.StoragePath, f.FileName })
            .FirstOrDefaultAsync(ct);
        if (file is null) return Results.NotFound();
        try
        {
            var stream = await storage.OpenReadAsync(file.StoragePath, ct);
            return Results.File(stream, "application/octet-stream", file.FileName);
        }
        catch (FileNotFoundException) { return Results.NotFound(); }
    }

    /// <summary>Global overview + phase-1 QA reports (missing document QA /
    /// missing grade-sheet QA), filtered by module start-date range.</summary>
    private static async Task<IResult> OverviewAsync(
        OdinDbContext db, CancellationToken ct,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? report = null)
    {
        var rows = await ModuleCohortLogic.ListAsync(db, partnerId, null, ct);
        var filtered = rows.Cast<dynamic>()
            .Where(r => from is null || (r.startDate != null && r.startDate >= Norm(from)))
            .Where(r => to is null || (r.startDate != null && r.startDate <= Norm(to)))
            .Where(r => report != "missing-doc-qa" || r.docQaDate == null)
            .Where(r => report != "missing-grade-qa" || r.gradeQaDate == null)
            .Cast<object>()
            .ToList();
        return Results.Ok(new { items = filtered });
    }

    /// <summary>Per-student cohort dropdowns: for every module of the
    /// enrolment's current specialization, the available cohorts and the
    /// currently assigned one.</summary>
    private static async Task<IResult> StudentCohortsAsync(
        Guid studentId, Guid enrollmentId, OdinDbContext db, CancellationToken ct)
    {
        var enrolment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null)
            .Select(e => new { e.PartnerId, e.SpecializationId })
            .FirstOrDefaultAsync(ct);
        if (enrolment is null) return Results.NotFound();

        var modules = await db.Subjects
            .Where(s => s.SpecializationId == enrolment.SpecializationId && s.DeletedAt == null)
            .OrderBy(s => s.Code)
            .Select(s => new { s.SubjectId, s.Code, s.Name })
            .ToListAsync(ct);
        var subjectIds = modules.Select(m => m.SubjectId).ToList();

        var cohorts = await db.ModuleCohorts
            .Where(c => c.DeletedAt == null && c.PartnerId == enrolment.PartnerId && subjectIds.Contains(c.SubjectId))
            .Select(c => new { c.ModuleCohortId, c.SubjectId, c.CohortNumber, c.StartDate, c.EndDate })
            .ToListAsync(ct);
        var cohortIds = cohorts.Select(c => c.ModuleCohortId).ToList();
        var assigned = await db.ModuleCohortStudents
            .Where(s => s.StudentEnrollmentId == enrollmentId && s.DeletedAt == null && cohortIds.Contains(s.ModuleCohortId))
            .Select(s => s.ModuleCohortId)
            .ToListAsync(ct);
        var assignedSet = assigned.ToHashSet();

        return Results.Ok(new
        {
            modules = modules.Select(m => new
            {
                subjectId = m.SubjectId,
                code = m.Code,
                name = m.Name,
                cohorts = cohorts.Where(c => c.SubjectId == m.SubjectId)
                    .Select(c => new
                    {
                        moduleCohortId = c.ModuleCohortId,
                        cohortNumber = c.CohortNumber,
                        startDate = c.StartDate,
                        endDate = c.EndDate,
                    }).ToList(),
                assignedCohortId = cohorts
                    .Where(c => c.SubjectId == m.SubjectId && assignedSet.Contains(c.ModuleCohortId))
                    .Select(c => (Guid?)c.ModuleCohortId)
                    .FirstOrDefault(),
            }).ToList(),
        });
    }

    private static async Task<IResult> SetStudentCohortAsync(
        Guid studentId, Guid enrollmentId, [FromBody] SetStudentCohortBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var enrolment = await db.Enrollments
            .Where(e => e.StudentEnrollmentId == enrollmentId && e.StudentId == studentId && e.DeletedAt == null)
            .Select(e => new { e.PartnerId })
            .FirstOrDefaultAsync(ct);
        if (enrolment is null) return Results.NotFound();

        // Cohorts of the same module for this partner — the enrolment sits in
        // at most one of them.
        var sameModuleCohortIds = await db.ModuleCohorts
            .Where(c => c.PartnerId == enrolment.PartnerId && c.SubjectId == body.SubjectId)
            .Select(c => c.ModuleCohortId)
            .ToListAsync(ct);
        var rows = await db.ModuleCohortStudents
            .Where(s => s.StudentEnrollmentId == enrollmentId && sameModuleCohortIds.Contains(s.ModuleCohortId))
            .ToListAsync(ct);
        foreach (var row in rows)
            row.DeletedAt = row.ModuleCohortId == body.CohortId ? null : (row.DeletedAt ?? DateTime.UtcNow);

        if (body.CohortId is { } target && rows.All(r => r.ModuleCohortId != target))
        {
            var valid = await db.ModuleCohorts.AnyAsync(c =>
                c.ModuleCohortId == target && c.PartnerId == enrolment.PartnerId
                && c.SubjectId == body.SubjectId && c.DeletedAt == null, ct);
            if (!valid) return Results.BadRequest(new { error = "Unknown cohort for this module." });
            db.ModuleCohortStudents.Add(new ModuleCohortStudent
            {
                ModuleCohortId = target,
                StudentEnrollmentId = enrollmentId,
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }
}
