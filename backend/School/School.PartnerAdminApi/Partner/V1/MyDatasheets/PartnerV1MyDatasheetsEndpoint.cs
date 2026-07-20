using Odin.Api.Base.Storage;
using School.PartnerAdminApi.Admin.V1.PartnerDatasheets;
using School.PartnerAdminApi.Partner.V1.MyUsers;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Partner.V1.MyDatasheets;

/// <summary>
/// Partner-portal datasheets (Faculties, Teachers, …): partners see every
/// sheet whose definition is "view" or "edit", and on "edit" definitions may
/// create sheets, add grid rows and fill exactly the fields flagged
/// PartnerCanEdit. MGW-only fields and system fields (auto id / computed
/// name) are read-only here; partners never delete rows or sheets. Teacher
/// partner-users are read-only via RolePathGuardMiddleware's write gate.
/// </summary>
[Route("/v1/partner/my/datasheets")]
[EndpointTag("Partner.MyDatasheets")]
public sealed class PartnerV1MyDatasheetsEndpoint : IEndpointMarker
{
    private const string StoragePrefix = "partner-datasheets/";

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my/datasheets", ListAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/datasheets", CreateAsync).RequireAuthorization("PartnerOnly");
        app.MapGet("/v1/partner/my/datasheets/{sheetId:guid}", GetAsync).RequireAuthorization("PartnerOnly");
        app.MapPut("/v1/partner/my/datasheets/{sheetId:guid}", SaveAsync).RequireAuthorization("PartnerOnly");
        app.MapPost("/v1/partner/my/datasheet-files", UploadFileAsync)
            .RequireAuthorization("PartnerOnly").DisableAntiforgery();
        app.MapGet("/v1/partner/my/datasheet-values/{valueId:guid}/file", DownloadFileAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct, [FromQuery] string? scope = null)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        // Faculty side matches Scope == faculty; datasheet side matches
        // everything else (legacy rows may carry an empty Scope).
        var faculty = scope == "faculty";
        var definitions = await db.PartnerDatasheetDefinitions
            .Where(d => d.DeletedAt == null && d.PartnerAccess != PartnerDatasheetDefinition.AccessHidden
                && (faculty
                    ? d.Scope == PartnerDatasheetDefinition.ScopeFaculty
                    : d.Scope != PartnerDatasheetDefinition.ScopeFaculty))
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new
            {
                partnerDatasheetDefinitionId = d.PartnerDatasheetDefinitionId,
                name = d.Name,
                partnerAccess = d.PartnerAccess,
            })
            .ToListAsync(ct);
        var visibleDefIds = definitions.Select(d => d.partnerDatasheetDefinitionId).ToList();

        var items = await db.PartnerDatasheets
            .Where(s => s.PartnerId == partnerId && s.DeletedAt == null && visibleDefIds.Contains(s.PartnerDatasheetDefinitionId))
            .OrderBy(s => s.CreatedAt)
            .Select(s => new
            {
                partnerDatasheetId = s.PartnerDatasheetId,
                definitionId = s.PartnerDatasheetDefinitionId,
                definitionName = db.PartnerDatasheetDefinitions
                    .Where(d => d.PartnerDatasheetDefinitionId == s.PartnerDatasheetDefinitionId)
                    .Select(d => d.Name).FirstOrDefault(),
                kind = s.Kind,
                parentId = s.ParentPartnerDatasheetId,
                partnerCanAddItems = s.PartnerCanAddItems,
                teacherUserId = s.TeacherUserId,
                title = s.Title,
                updatedAt = s.UpdatedAt ?? s.CreatedAt,
            })
            .ToListAsync(ct);

        return Results.Ok(new { items, definitions });
    }

    private static async Task<IResult> CreateAsync(
        HttpContext httpContext, [FromBody] AdminV1PartnerDatasheetsEndpoint.CreateBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        // Items inherit the definition from their group; either way the
        // definition must be partner-editable.
        Guid definitionId;
        var kind = PartnerDatasheet.KindStandalone;
        Guid? parentId = null;
        if (body.ParentId is { } pid)
        {
            var group = await db.PartnerDatasheets.FirstOrDefaultAsync(s =>
                s.PartnerDatasheetId == pid && s.PartnerId == partnerId
                && s.Kind == PartnerDatasheet.KindGroup && s.DeletedAt == null, ct);
            if (group is null) return Results.BadRequest(new { error = "Unknown group." });
            if (!group.PartnerCanAddItems)
                return Results.BadRequest(new { error = "The Admission Office has not opened this group for partner additions." });
            definitionId = group.PartnerDatasheetDefinitionId;
            kind = PartnerDatasheet.KindItem;
            parentId = pid;
        }
        else
        {
            definitionId = body.DefinitionId ?? Guid.Empty;
            if (string.Equals(body.Kind, PartnerDatasheet.KindGroup, StringComparison.OrdinalIgnoreCase))
                kind = PartnerDatasheet.KindGroup;
        }
        var editable = await db.PartnerDatasheetDefinitions.AnyAsync(d =>
            d.PartnerDatasheetDefinitionId == definitionId && d.DeletedAt == null
            && d.PartnerAccess == PartnerDatasheetDefinition.AccessEdit, ct);
        if (!editable) return Results.BadRequest(new { error = "This datasheet cannot be created from the partner portal." });

        var sheet = new PartnerDatasheet
        {
            PartnerId = partnerId!.Value,
            PartnerDatasheetDefinitionId = definitionId,
            Kind = kind,
            ParentPartnerDatasheetId = parentId,
            // A group the partner created themselves stays open for them.
            PartnerCanAddItems = kind == PartnerDatasheet.KindGroup,
            Title = string.IsNullOrWhiteSpace(body.Title) ? null : body.Title.Trim(),
        };
        db.PartnerDatasheets.Add(sheet);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerDatasheetId = sheet.PartnerDatasheetId });
    }

    private static async Task<(Guid PartnerId, PartnerDatasheet? Sheet, string Access)> ResolveSheetAsync(
        HttpContext httpContext, Guid sheetId, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null || partnerId is null) return (Guid.Empty, null, PartnerDatasheetDefinition.AccessHidden);
        var sheet = await db.PartnerDatasheets
            .FirstOrDefaultAsync(s => s.PartnerDatasheetId == sheetId && s.PartnerId == partnerId && s.DeletedAt == null, ct);
        if (sheet is null) return (partnerId.Value, null, PartnerDatasheetDefinition.AccessHidden);
        var access = await db.PartnerDatasheetDefinitions
            .Where(d => d.PartnerDatasheetDefinitionId == sheet.PartnerDatasheetDefinitionId && d.DeletedAt == null)
            .Select(d => d.PartnerAccess)
            .FirstOrDefaultAsync(ct) ?? PartnerDatasheetDefinition.AccessHidden;
        return (partnerId.Value, sheet, access);
    }

    private static async Task<IResult> GetAsync(
        Guid sheetId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, sheet, access) = await ResolveSheetAsync(httpContext, sheetId, db, ct);
        if (sheet is null || access == PartnerDatasheetDefinition.AccessHidden) return Results.NotFound();

        var definitionName = await db.PartnerDatasheetDefinitions
            .Where(d => d.PartnerDatasheetDefinitionId == sheet.PartnerDatasheetDefinitionId)
            .Select(d => d.Name).FirstOrDefaultAsync(ct) ?? "Datasheet";
        var sections = await db.PartnerDatasheetSections
            .Where(s => s.PartnerDatasheetDefinitionId == sheet.PartnerDatasheetDefinitionId && s.DeletedAt == null)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
        var sectionIds = sections.Select(s => s.PartnerDatasheetSectionId).ToList();
        var fields = await db.PartnerDatasheetFields
            .Where(f => sectionIds.Contains(f.PartnerDatasheetSectionId) && f.DeletedAt == null)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);
        var rows = await db.PartnerDatasheetRows
            .Where(r => r.PartnerDatasheetId == sheetId && r.DeletedAt == null)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(ct);
        var rowIds = rows.Select(r => r.PartnerDatasheetRowId).ToList();
        var values = await db.PartnerDatasheetValues
            .Where(v => rowIds.Contains(v.PartnerDatasheetRowId))
            .ToListAsync(ct);

        var canEdit = access == PartnerDatasheetDefinition.AccessEdit;
        return Results.Ok(new
        {
            partnerDatasheetId = sheet.PartnerDatasheetId,
            title = sheet.Title,
            definitionName,
            partnerAccess = access,
            sections = sections.Select(s => new
            {
                id = s.PartnerDatasheetSectionId,
                title = s.Title,
                kind = s.Kind,
                fields = fields
                    .Where(f => f.PartnerDatasheetSectionId == s.PartnerDatasheetSectionId)
                    .Select(f => new
                    {
                        id = f.PartnerDatasheetFieldId,
                        label = f.Label,
                        type = f.Type,
                        options = f.Type == PartnerDatasheetField.TypeSelect
                            ? (f.OptionsText ?? string.Empty)
                                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                            : [],
                        isRequired = f.IsRequired,
                        partnerCanEdit = canEdit && f.PartnerCanEdit,
                    })
                    .ToList(),
            }).ToList(),
            rows = rows.Select(r => new
            {
                id = r.PartnerDatasheetRowId,
                sectionId = r.PartnerDatasheetSectionId,
                values = values
                    .Where(v => v.PartnerDatasheetRowId == r.PartnerDatasheetRowId)
                    .ToDictionary(
                        v => v.PartnerDatasheetFieldId.ToString(),
                        v => new { valueId = v.PartnerDatasheetValueId, value = v.Value, fileName = v.FileName }),
            }).ToList(),
        });
    }

    /// <summary>
    /// Partner save: existing rows update, new rows append; rows missing from
    /// the payload are KEPT (partners never delete data). Only fields with
    /// PartnerCanEdit take values; everything else is ignored server-side.
    /// </summary>
    private static async Task<IResult> SaveAsync(
        Guid sheetId, HttpContext httpContext, [FromBody] AdminV1PartnerDatasheetsEndpoint.SaveBody body,
        OdinDbContext db, CancellationToken ct)
    {
        var (partnerId, sheet, access) = await ResolveSheetAsync(httpContext, sheetId, db, ct);
        if (sheet is null || access == PartnerDatasheetDefinition.AccessHidden) return Results.NotFound();
        if (access != PartnerDatasheetDefinition.AccessEdit)
            return Results.StatusCode(403);

        var validSectionIds = (await db.PartnerDatasheetSections
            .Where(s => s.PartnerDatasheetDefinitionId == sheet.PartnerDatasheetDefinitionId && s.DeletedAt == null)
            .Select(s => s.PartnerDatasheetSectionId)
            .ToListAsync(ct)).ToHashSet();
        var editableFieldIds = (await db.PartnerDatasheetFields
            .Where(f => validSectionIds.Contains(f.PartnerDatasheetSectionId) && f.DeletedAt == null
                && f.PartnerCanEdit
                && f.Type != PartnerDatasheetField.TypeAutoId && f.Type != PartnerDatasheetField.TypeComputed)
            .Select(f => f.PartnerDatasheetFieldId)
            .ToListAsync(ct)).ToHashSet();

        var existingRows = await db.PartnerDatasheetRows
            .Where(r => r.PartnerDatasheetId == sheetId)
            .ToListAsync(ct);
        var existingRowIds = existingRows.Select(r => r.PartnerDatasheetRowId).ToList();
        var existingValues = await db.PartnerDatasheetValues
            .Where(v => existingRowIds.Contains(v.PartnerDatasheetRowId))
            .ToListAsync(ct);

        var order = existingRows.Count;
        foreach (var r in body.Rows ?? [])
        {
            if (!validSectionIds.Contains(r.SectionId)) continue;
            var row = r.Id is { } rid ? existingRows.FirstOrDefault(x => x.PartnerDatasheetRowId == rid) : null;
            if (row is null)
            {
                row = new PartnerDatasheetRow
                {
                    PartnerDatasheetId = sheetId,
                    PartnerDatasheetSectionId = r.SectionId,
                    SortOrder = order++,
                };
                db.PartnerDatasheetRows.Add(row);
                existingRows.Add(row);
            }

            foreach (var (fieldIdRaw, cell) in r.Values ?? [])
            {
                if (!Guid.TryParse(fieldIdRaw, out var fieldId) || !editableFieldIds.Contains(fieldId)) continue;
                var existing = existingValues.FirstOrDefault(v =>
                    v.PartnerDatasheetRowId == row.PartnerDatasheetRowId && v.PartnerDatasheetFieldId == fieldId);
                if (string.IsNullOrWhiteSpace(cell?.Value))
                {
                    if (existing is not null) db.PartnerDatasheetValues.Remove(existing);
                    continue;
                }
                if (existing is null)
                {
                    existing = new PartnerDatasheetValue
                    {
                        PartnerDatasheetRowId = row.PartnerDatasheetRowId,
                        PartnerDatasheetFieldId = fieldId,
                    };
                    db.PartnerDatasheetValues.Add(existing);
                    existingValues.Add(existing);
                }
                existing.Value = cell!.Value!.Trim();
                existing.FileName = string.IsNullOrWhiteSpace(cell.FileName) ? null : cell.FileName.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (!string.IsNullOrWhiteSpace(body.Title)) sheet.Title = body.Title.Trim();
        sheet.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await PartnerDatasheetSystemValues.ApplyAsync(db, sheetId, partnerId, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> UploadFileAsync(
        HttpContext httpContext, IFormFile file, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (_, _, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "file is required" });
        if (file.Length > 50 * 1024 * 1024)
            return Results.BadRequest(new { error = "Max file size is 50 MB." });

        var safeName = Path.GetFileName(file.FileName);
        string storagePath;
        await using (var stream = file.OpenReadStream())
        {
            storagePath = await storage.SaveAsync(stream, $"{StoragePrefix}{Guid.NewGuid():N}-{safeName}", ct);
        }
        return Results.Ok(new { token = storagePath, fileName = safeName });
    }

    private static async Task<IResult> DownloadFileAsync(
        Guid valueId, HttpContext httpContext, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var value = await (
            from v in db.PartnerDatasheetValues
            join r in db.PartnerDatasheetRows on v.PartnerDatasheetRowId equals r.PartnerDatasheetRowId
            join s in db.PartnerDatasheets on r.PartnerDatasheetId equals s.PartnerDatasheetId
            where v.PartnerDatasheetValueId == valueId && s.PartnerId == partnerId && s.DeletedAt == null
            select new { v.Value, v.FileName }).FirstOrDefaultAsync(ct);
        if (value is null || !value.Value.Contains(StoragePrefix)) return Results.NotFound();

        try
        {
            var stream = await storage.OpenReadAsync(value.Value, ct);
            return Results.File(stream, "application/octet-stream", value.FileName ?? "datasheet-file");
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
