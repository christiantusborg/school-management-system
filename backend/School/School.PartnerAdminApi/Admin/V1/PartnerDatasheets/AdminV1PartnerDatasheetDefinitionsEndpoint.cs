using Odin.Api.Base.Authorization;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.PartnerDatasheets;

/// <summary>
/// System Config → Partner Datasheets: reusable datasheet templates (the
/// core "Pages/Datasheet" concept, fully relational). A definition holds
/// sections ("fields" or "grid"), each holding typed fields. Structure edits
/// are soft-delete based: removing a field hides it everywhere but keeps all
/// stored values; re-adding a field with the same label restores them.
/// </summary>
[Route("/v1/admin/partner-datasheet-definitions")]
[EndpointTag("Admin.PartnerDatasheetDefinitions")]
public sealed class AdminV1PartnerDatasheetDefinitionsEndpoint : IEndpointMarker
{
    private static readonly string[] Kinds = [PartnerDatasheetSection.KindFields, PartnerDatasheetSection.KindGrid];
    private static readonly string[] FieldTypes =
    [
        PartnerDatasheetField.TypeText, PartnerDatasheetField.TypeNumber, PartnerDatasheetField.TypeDate,
        PartnerDatasheetField.TypeFile, PartnerDatasheetField.TypeFiles, PartnerDatasheetField.TypeSelect,
        PartnerDatasheetField.TypeBool, PartnerDatasheetField.TypeAutoId, PartnerDatasheetField.TypeComputed,
    ];
    private static readonly string[] AccessLevels =
    [
        PartnerDatasheetDefinition.AccessHidden, PartnerDatasheetDefinition.AccessView, PartnerDatasheetDefinition.AccessEdit,
    ];
    // System types keep their OptionsText (pattern/template) and are never partner-editable.
    private static bool IsSystemType(string? t) =>
        t is PartnerDatasheetField.TypeAutoId or PartnerDatasheetField.TypeComputed;

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/partner-datasheet-definitions", ListAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/partner-datasheet-definitions", CreateAsync).RequireAuthorization("AdminOnly");
        app.MapPatch("/v1/admin/partner-datasheet-definitions/{definitionId:guid}", RenameAsync).RequireAuthorization("AdminOnly");
        app.MapPut("/v1/admin/partner-datasheet-definitions/{definitionId:guid}/structure", SaveStructureAsync).RequireAuthorization("AdminOnly");
        app.MapDelete("/v1/admin/partner-datasheet-definitions/{definitionId:guid}", DeleteAsync).RequireAuthorization("AdminOnly");
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

    private static async Task<IResult> ListAsync(OdinDbContext db, CancellationToken ct)
    {
        // != faculty (not == datasheet) so legacy rows with an empty Scope
        // keep behaving as ordinary datasheets.
        var defs = await db.PartnerDatasheetDefinitions
            .Where(d => d.DeletedAt == null && d.Scope != PartnerDatasheetDefinition.ScopeFaculty)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new
            {
                d.PartnerDatasheetDefinitionId,
                d.Name,
                d.PartnerAccess,
                UpdatedAt = d.UpdatedAt ?? d.CreatedAt,
                InUse = db.PartnerDatasheets.Count(s => s.PartnerDatasheetDefinitionId == d.PartnerDatasheetDefinitionId && s.DeletedAt == null),
            })
            .ToListAsync(ct);

        var defIds = defs.Select(d => d.PartnerDatasheetDefinitionId).ToList();
        var sections = await db.PartnerDatasheetSections
            .Where(s => defIds.Contains(s.PartnerDatasheetDefinitionId) && s.DeletedAt == null)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
        var sectionIds = sections.Select(s => s.PartnerDatasheetSectionId).ToList();
        var fields = await db.PartnerDatasheetFields
            .Where(f => sectionIds.Contains(f.PartnerDatasheetSectionId) && f.DeletedAt == null)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        var items = defs.Select(d => new
        {
            partnerDatasheetDefinitionId = d.PartnerDatasheetDefinitionId,
            name = d.Name,
            partnerAccess = d.PartnerAccess,
            inUse = d.InUse,
            updatedAt = d.UpdatedAt,
            sections = sections
                .Where(s => s.PartnerDatasheetDefinitionId == d.PartnerDatasheetDefinitionId)
                .Select(s => new
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
                            optionsText = f.OptionsText,
                            tooltip = f.Tooltip,
                            isRequired = f.IsRequired,
                            partnerCanEdit = f.PartnerCanEdit,
                        })
                        .ToList(),
                })
                .ToList(),
        }).ToList();

        return Results.Ok(new { items });
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] Dictionary<string, string?> body, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "config.datasheet_defs", ct) != AccessLevel.Edit) return Results.Forbid();

        var name = body.GetValueOrDefault("name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });
        var access = body.GetValueOrDefault("partnerAccess") ?? PartnerDatasheetDefinition.AccessHidden;
        if (!AccessLevels.Contains(access))
            return Results.BadRequest(new { error = "Unknown partner access level." });
        var def = new PartnerDatasheetDefinition { Name = name, PartnerAccess = access };
        db.PartnerDatasheetDefinitions.Add(def);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerDatasheetDefinitionId = def.PartnerDatasheetDefinitionId });
    }

    private static async Task<IResult> RenameAsync(
        Guid definitionId, [FromBody] Dictionary<string, string?> body, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "config.datasheet_defs", ct) != AccessLevel.Edit) return Results.Forbid();

        var def = await db.PartnerDatasheetDefinitions
            .FirstOrDefaultAsync(d => d.PartnerDatasheetDefinitionId == definitionId && d.DeletedAt == null, ct);
        if (def is null) return Results.NotFound();
        var name = body.GetValueOrDefault("name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Name is required." });
        var access = body.GetValueOrDefault("partnerAccess");
        if (access is not null)
        {
            if (!AccessLevels.Contains(access))
                return Results.BadRequest(new { error = "Unknown partner access level." });
            def.PartnerAccess = access;
        }
        def.Name = name;
        def.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerDatasheetDefinitionId = definitionId });
    }

    /// <summary>
    /// Wholesale structure save. Sections/fields present in the payload are
    /// updated (matched by id) or created; ones missing from the payload are
    /// SOFT-deleted so their stored values survive. A new field whose label
    /// matches a soft-deleted sibling restores that field — and its data.
    /// </summary>
    private static async Task<IResult> SaveStructureAsync(
        Guid definitionId, [FromBody] StructureBody body, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "config.datasheet_defs", ct) != AccessLevel.Edit) return Results.Forbid();

        var def = await db.PartnerDatasheetDefinitions
            .FirstOrDefaultAsync(d => d.PartnerDatasheetDefinitionId == definitionId && d.DeletedAt == null, ct);
        if (def is null) return Results.NotFound();

        var incoming = body.Sections ?? [];
        foreach (var s in incoming)
        {
            if (string.IsNullOrWhiteSpace(s.Title))
                return Results.BadRequest(new { error = "Every section needs a title." });
            if (!Kinds.Contains(s.Kind))
                return Results.BadRequest(new { error = $"Unknown section kind '{s.Kind}'." });
            foreach (var f in s.Fields ?? [])
            {
                if (string.IsNullOrWhiteSpace(f.Label))
                    return Results.BadRequest(new { error = $"Every field in \"{s.Title}\" needs a label." });
                if (!FieldTypes.Contains(f.Type))
                    return Results.BadRequest(new { error = $"Unknown field type '{f.Type}'." });
            }
        }

        var allSections = await db.PartnerDatasheetSections
            .Where(s => s.PartnerDatasheetDefinitionId == definitionId)
            .ToListAsync(ct);
        var allSectionIds = allSections.Select(s => s.PartnerDatasheetSectionId).ToList();
        var allFields = await db.PartnerDatasheetFields
            .Where(f => allSectionIds.Contains(f.PartnerDatasheetSectionId))
            .ToListAsync(ct);

        var keptSectionIds = new HashSet<Guid>();
        var sectionOrder = 0;
        foreach (var s in incoming)
        {
            var section = s.Id is { } sid ? allSections.FirstOrDefault(x => x.PartnerDatasheetSectionId == sid) : null;
            if (section is null)
            {
                section = new PartnerDatasheetSection { PartnerDatasheetDefinitionId = definitionId };
                db.PartnerDatasheetSections.Add(section);
                allSections.Add(section);
            }
            section.Title = s.Title!.Trim();
            section.Kind = s.Kind!;
            section.SortOrder = sectionOrder++;
            section.DeletedAt = null;
            keptSectionIds.Add(section.PartnerDatasheetSectionId);

            var sectionFields = allFields.Where(f => f.PartnerDatasheetSectionId == section.PartnerDatasheetSectionId).ToList();
            var keptFieldIds = new HashSet<Guid>();
            var fieldOrder = 0;
            foreach (var f in s.Fields ?? [])
            {
                var field = f.Id is { } fid ? sectionFields.FirstOrDefault(x => x.PartnerDatasheetFieldId == fid) : null;
                // Re-adding a removed field by label restores it (and its values).
                field ??= sectionFields.FirstOrDefault(x =>
                    x.DeletedAt != null && string.Equals(x.Label, f.Label!.Trim(), StringComparison.OrdinalIgnoreCase));
                if (field is null)
                {
                    field = new PartnerDatasheetField { PartnerDatasheetSectionId = section.PartnerDatasheetSectionId };
                    db.PartnerDatasheetFields.Add(field);
                    allFields.Add(field);
                    sectionFields.Add(field);
                }
                field.Label = f.Label!.Trim();
                field.Type = f.Type!;
                field.OptionsText = f.Type == PartnerDatasheetField.TypeSelect || IsSystemType(f.Type)
                    ? f.OptionsText
                    : null;
                field.Tooltip = string.IsNullOrWhiteSpace(f.Tooltip) ? null : f.Tooltip.Trim();
                field.IsRequired = f.IsRequired;
                field.PartnerCanEdit = f.PartnerCanEdit && !IsSystemType(f.Type);
                field.SortOrder = fieldOrder++;
                field.DeletedAt = null;
                keptFieldIds.Add(field.PartnerDatasheetFieldId);
            }
            foreach (var f in sectionFields.Where(x => x.DeletedAt == null && !keptFieldIds.Contains(x.PartnerDatasheetFieldId)))
                f.DeletedAt = DateTime.UtcNow;
        }
        foreach (var s in allSections.Where(x => x.DeletedAt == null && !keptSectionIds.Contains(x.PartnerDatasheetSectionId)))
            s.DeletedAt = DateTime.UtcNow;

        def.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { saved = true });
    }

    private static async Task<IResult> DeleteAsync(Guid definitionId, HttpContext httpContext, IPermissionService perms, OdinDbContext db, CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "config.datasheet_defs", ct) != AccessLevel.Edit) return Results.Forbid();

        var def = await db.PartnerDatasheetDefinitions
            .FirstOrDefaultAsync(d => d.PartnerDatasheetDefinitionId == definitionId && d.DeletedAt == null, ct);
        if (def is null) return Results.NotFound();
        var inUse = await db.PartnerDatasheets
            .CountAsync(s => s.PartnerDatasheetDefinitionId == definitionId && s.DeletedAt == null, ct);
        if (inUse > 0)
            return Results.Conflict(new { error = $"This datasheet is used on {inUse} partner(s). Remove those sheets first." });
        def.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true });
    }
}
