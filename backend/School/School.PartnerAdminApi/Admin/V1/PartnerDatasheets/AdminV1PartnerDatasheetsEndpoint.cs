using Odin.Api.Base.Storage;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.PartnerDatasheets;

/// <summary>
/// Datasheets attached to ONE partner: extra structured data the Admission
/// Office keeps per partner. Attach any definition any number of times (each
/// sheet gets its own title); data is stored relationally — one row per
/// grid line, one value per cell — never as JSON.
/// </summary>
[Route("/v1/admin/partners/{partnerId:guid}/datasheets")]
[EndpointTag("Admin.PartnerDatasheets")]
public sealed class AdminV1PartnerDatasheetsEndpoint : IEndpointMarker
{
    private const string StoragePrefix = "partner-datasheets/";

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/partners/{partnerId:guid}/datasheets", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partners/{partnerId:guid}/datasheets", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/partner-datasheets/{sheetId:guid}", GetAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/partner-datasheets/{sheetId:guid}", SaveAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/partner-datasheets/{sheetId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partner-datasheet-files", UploadFileAsync)
            .RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapGet("/v1/admin/partner-datasheet-values/{valueId:guid}/file", DownloadFileAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    public sealed class CreateBody
    {
        public Guid? DefinitionId { get; init; }
        public string? Title { get; init; }
    }

    public sealed class CellDto
    {
        public string? Value { get; init; }
        public string? FileName { get; init; }
    }

    public sealed class RowDto
    {
        public Guid? Id { get; init; }
        public Guid SectionId { get; init; }
        public Dictionary<string, CellDto>? Values { get; init; }
    }

    public sealed class SaveBody
    {
        public string? Title { get; init; }
        public List<RowDto>? Rows { get; init; }
    }

    private static async Task<IResult> ListAsync(Guid partnerId, OdinDbContext db, CancellationToken ct)
    {
        var partnerExists = await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (!partnerExists) return Results.NotFound();

        var items = await db.PartnerDatasheets
            .Where(s => s.PartnerId == partnerId && s.DeletedAt == null)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new
            {
                partnerDatasheetId = s.PartnerDatasheetId,
                definitionId = s.PartnerDatasheetDefinitionId,
                definitionName = db.PartnerDatasheetDefinitions
                    .Where(d => d.PartnerDatasheetDefinitionId == s.PartnerDatasheetDefinitionId)
                    .Select(d => d.Name).FirstOrDefault(),
                title = s.Title,
                updatedAt = s.UpdatedAt ?? s.CreatedAt,
            })
            .ToListAsync(ct);

        var definitions = await db.PartnerDatasheetDefinitions
            .Where(d => d.DeletedAt == null)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new { partnerDatasheetDefinitionId = d.PartnerDatasheetDefinitionId, name = d.Name })
            .ToListAsync(ct);

        return Results.Ok(new { items, definitions });
    }

    private static async Task<IResult> CreateAsync(
        Guid partnerId, [FromBody] CreateBody body, OdinDbContext db, CancellationToken ct)
    {
        var partnerExists = await db.Partners.AnyAsync(p => p.PartnerId == partnerId && p.DeletedAt == null, ct);
        if (!partnerExists) return Results.NotFound();
        var defExists = await db.PartnerDatasheetDefinitions.AnyAsync(d =>
            d.PartnerDatasheetDefinitionId == body.DefinitionId && d.DeletedAt == null, ct);
        if (!defExists) return Results.BadRequest(new { error = "Unknown datasheet definition." });

        var sheet = new PartnerDatasheet
        {
            PartnerId = partnerId,
            PartnerDatasheetDefinitionId = body.DefinitionId!.Value,
            Title = string.IsNullOrWhiteSpace(body.Title) ? null : body.Title.Trim(),
        };
        db.PartnerDatasheets.Add(sheet);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerDatasheetId = sheet.PartnerDatasheetId });
    }

    /// <summary>Full sheet: live structure + rows + cell values.</summary>
    private static async Task<IResult> GetAsync(Guid sheetId, OdinDbContext db, CancellationToken ct)
    {
        var sheet = await db.PartnerDatasheets
            .Where(s => s.PartnerDatasheetId == sheetId && s.DeletedAt == null)
            .Select(s => new { s.PartnerDatasheetId, s.PartnerDatasheetDefinitionId, s.Title })
            .FirstOrDefaultAsync(ct);
        if (sheet is null) return Results.NotFound();

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

        return Results.Ok(new
        {
            partnerDatasheetId = sheet.PartnerDatasheetId,
            title = sheet.Title,
            definitionName,
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
                        options = (f.OptionsText ?? string.Empty)
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                        isRequired = f.IsRequired,
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
    /// Wholesale save: rows in the payload are updated (by id) or created in
    /// payload order; rows missing from the payload are deleted with their
    /// values (removing a grid line is intentional). Cells upsert per
    /// (row, field); an empty value deletes the cell.
    /// </summary>
    private static async Task<IResult> SaveAsync(
        Guid sheetId, [FromBody] SaveBody body, OdinDbContext db, CancellationToken ct)
    {
        var sheet = await db.PartnerDatasheets
            .FirstOrDefaultAsync(s => s.PartnerDatasheetId == sheetId && s.DeletedAt == null, ct);
        if (sheet is null) return Results.NotFound();

        var validSectionIds = (await db.PartnerDatasheetSections
            .Where(s => s.PartnerDatasheetDefinitionId == sheet.PartnerDatasheetDefinitionId && s.DeletedAt == null)
            .Select(s => s.PartnerDatasheetSectionId)
            .ToListAsync(ct)).ToHashSet();
        var validFieldIds = (await db.PartnerDatasheetFields
            .Where(f => validSectionIds.Contains(f.PartnerDatasheetSectionId) && f.DeletedAt == null)
            .Select(f => f.PartnerDatasheetFieldId)
            .ToListAsync(ct)).ToHashSet();

        var existingRows = await db.PartnerDatasheetRows
            .Where(r => r.PartnerDatasheetId == sheetId)
            .ToListAsync(ct);
        var existingRowIds = existingRows.Select(r => r.PartnerDatasheetRowId).ToList();
        var existingValues = await db.PartnerDatasheetValues
            .Where(v => existingRowIds.Contains(v.PartnerDatasheetRowId))
            .ToListAsync(ct);

        var keptRowIds = new HashSet<Guid>();
        var order = 0;
        foreach (var r in body.Rows ?? [])
        {
            if (!validSectionIds.Contains(r.SectionId))
                return Results.BadRequest(new { error = "Row references an unknown section." });

            var row = r.Id is { } rid ? existingRows.FirstOrDefault(x => x.PartnerDatasheetRowId == rid) : null;
            if (row is null)
            {
                row = new PartnerDatasheetRow { PartnerDatasheetId = sheetId, PartnerDatasheetSectionId = r.SectionId };
                db.PartnerDatasheetRows.Add(row);
                existingRows.Add(row);
            }
            row.SortOrder = order++;
            row.DeletedAt = null;
            keptRowIds.Add(row.PartnerDatasheetRowId);

            foreach (var (fieldIdRaw, cell) in r.Values ?? [])
            {
                if (!Guid.TryParse(fieldIdRaw, out var fieldId) || !validFieldIds.Contains(fieldId)) continue;
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

        foreach (var row in existingRows.Where(x => !keptRowIds.Contains(x.PartnerDatasheetRowId)).ToList())
        {
            db.PartnerDatasheetValues.RemoveRange(
                existingValues.Where(v => v.PartnerDatasheetRowId == row.PartnerDatasheetRowId));
            db.PartnerDatasheetRows.Remove(row);
        }

        sheet.Title = string.IsNullOrWhiteSpace(body.Title) ? sheet.Title : body.Title.Trim();
        sheet.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> DeleteAsync(Guid sheetId, OdinDbContext db, CancellationToken ct)
    {
        var sheet = await db.PartnerDatasheets
            .FirstOrDefaultAsync(s => s.PartnerDatasheetId == sheetId && s.DeletedAt == null, ct);
        if (sheet is null) return Results.NotFound();
        sheet.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }

    /// <summary>
    /// Uploads a file for a "file" cell; the returned token goes into the
    /// cell's value on the next sheet save. Any file type, 50 MB cap.
    /// </summary>
    private static async Task<IResult> UploadFileAsync(
        IFormFile file, IFileStorage storage, CancellationToken ct)
    {
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
        Guid valueId, OdinDbContext db, IFileStorage storage, CancellationToken ct)
    {
        var value = await db.PartnerDatasheetValues
            .Where(v => v.PartnerDatasheetValueId == valueId)
            .Select(v => new { v.Value, v.FileName })
            .FirstOrDefaultAsync(ct);
        // Only paths minted by UploadFileAsync are servable — the value is
        // client-supplied on save, so anything else must not touch storage.
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
