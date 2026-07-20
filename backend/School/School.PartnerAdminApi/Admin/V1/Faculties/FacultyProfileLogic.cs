using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.Faculties;

/// <summary>
/// Shared logic of the Faculties feature (its own tables — NOT datasheets):
/// structure projection, profile save reconciliation and the system field
/// types. "autoid" issues one-time ids from the field's pattern
/// ("MGW-ALC-FAC-{partner}-{n}", numbered per partner, never reissued);
/// "computed" rebuilds from sibling values ("{First name} {Last name}").
/// </summary>
public static class FacultyProfileLogic
{
    public const string StoragePrefix = "faculty-profiles/";

    public static readonly string[] Kinds = [FacultyProfileSection.KindFields, FacultyProfileSection.KindGrid];
    public static readonly string[] FieldTypes =
    [
        FacultyProfileField.TypeText, FacultyProfileField.TypeNumber, FacultyProfileField.TypeDate,
        FacultyProfileField.TypeFile, FacultyProfileField.TypeSelect, FacultyProfileField.TypeBool,
        FacultyProfileField.TypeAutoId, FacultyProfileField.TypeComputed,
    ];

    public static bool IsSystemType(string? t) =>
        t is FacultyProfileField.TypeAutoId or FacultyProfileField.TypeComputed;

    /// <summary>Live structure with per-field metadata, shared by every
    /// profile endpoint (admin + partner).</summary>
    public static async Task<List<object>> StructureAsync(OdinDbContext db, CancellationToken ct)
    {
        var sections = await db.FacultyProfileSections
            .Where(s => s.DeletedAt == null)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
        var fields = await db.FacultyProfileFields
            .Where(f => f.DeletedAt == null)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);
        return sections.Select(s => (object)new
        {
            id = s.FacultyProfileSectionId,
            title = s.Title,
            kind = s.Kind,
            fields = fields
                .Where(f => f.FacultyProfileSectionId == s.FacultyProfileSectionId)
                .Select(f => new
                {
                    id = f.FacultyProfileFieldId,
                    label = f.Label,
                    type = f.Type,
                    optionsText = f.OptionsText,
                    options = f.Type == FacultyProfileField.TypeSelect
                        ? (f.OptionsText ?? string.Empty)
                            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        : [],
                    isRequired = f.IsRequired,
                    partnerCanEdit = f.PartnerCanEdit,
                })
                .ToList(),
        }).ToList();
    }

    /// <summary>Full profile payload for one teacher.</summary>
    public static async Task<object?> ProfileAsync(OdinDbContext db, Guid teacherId, CancellationToken ct)
    {
        var teacher = await db.Teachers
            .Where(t => t.TeacherId == teacherId && t.DeletedAt == null)
            .Select(t => new { t.TeacherId, t.PartnerId, t.DisplayName, t.UserId })
            .FirstOrDefaultAsync(ct);
        if (teacher is null) return null;

        var rows = await db.TeacherProfileRows
            .Where(r => r.TeacherId == teacherId && r.DeletedAt == null)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(ct);
        var rowIds = rows.Select(r => r.TeacherProfileRowId).ToList();
        var values = await db.TeacherProfileValues
            .Where(v => rowIds.Contains(v.TeacherProfileRowId))
            .ToListAsync(ct);

        return new
        {
            teacherId = teacher.TeacherId,
            partnerId = teacher.PartnerId,
            displayName = teacher.DisplayName,
            userId = teacher.UserId,
            sections = await StructureAsync(db, ct),
            rows = rows.Select(r => new
            {
                id = r.TeacherProfileRowId,
                sectionId = r.FacultyProfileSectionId,
                values = values
                    .Where(v => v.TeacherProfileRowId == r.TeacherProfileRowId)
                    .ToDictionary(
                        v => v.FacultyProfileFieldId.ToString(),
                        v => new { valueId = v.TeacherProfileValueId, value = v.Value, fileName = v.FileName }),
            }).ToList(),
        };
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
        public string? DisplayName { get; init; }
        public List<RowDto>? Rows { get; init; }
    }

    /// <summary>
    /// Profile save. Admin path (partnerEditableOnly=false): rows update or
    /// create, rows missing from the payload are deleted. Partner path
    /// (partnerEditableOnly=true): only PartnerCanEdit fields take values,
    /// rows append but never delete. System fields are always regenerated.
    /// </summary>
    public static async Task<bool> SaveProfileAsync(
        OdinDbContext db, Guid teacherId, SaveBody body, bool partnerEditableOnly, CancellationToken ct)
    {
        var teacher = await db.Teachers
            .FirstOrDefaultAsync(t => t.TeacherId == teacherId && t.DeletedAt == null, ct);
        if (teacher is null) return false;

        var validSectionIds = (await db.FacultyProfileSections
            .Where(s => s.DeletedAt == null)
            .Select(s => s.FacultyProfileSectionId)
            .ToListAsync(ct)).ToHashSet();
        var writableFieldIds = (await db.FacultyProfileFields
            .Where(f => f.DeletedAt == null
                && f.Type != FacultyProfileField.TypeAutoId && f.Type != FacultyProfileField.TypeComputed
                && (!partnerEditableOnly || f.PartnerCanEdit))
            .Select(f => f.FacultyProfileFieldId)
            .ToListAsync(ct)).ToHashSet();

        var existingRows = await db.TeacherProfileRows
            .Where(r => r.TeacherId == teacherId)
            .ToListAsync(ct);
        var existingRowIds = existingRows.Select(r => r.TeacherProfileRowId).ToList();
        var existingValues = await db.TeacherProfileValues
            .Where(v => existingRowIds.Contains(v.TeacherProfileRowId))
            .ToListAsync(ct);

        var keptRowIds = new HashSet<Guid>();
        var order = 0;
        foreach (var r in body.Rows ?? [])
        {
            if (!validSectionIds.Contains(r.SectionId)) continue;
            var row = r.Id is { } rid ? existingRows.FirstOrDefault(x => x.TeacherProfileRowId == rid) : null;
            if (row is null)
            {
                row = new TeacherProfileRow { TeacherId = teacherId, FacultyProfileSectionId = r.SectionId };
                db.TeacherProfileRows.Add(row);
                existingRows.Add(row);
            }
            row.SortOrder = order++;
            row.DeletedAt = null;
            keptRowIds.Add(row.TeacherProfileRowId);

            foreach (var (fieldIdRaw, cell) in r.Values ?? [])
            {
                if (!Guid.TryParse(fieldIdRaw, out var fieldId) || !writableFieldIds.Contains(fieldId)) continue;
                var existing = existingValues.FirstOrDefault(v =>
                    v.TeacherProfileRowId == row.TeacherProfileRowId && v.FacultyProfileFieldId == fieldId);
                if (string.IsNullOrWhiteSpace(cell?.Value))
                {
                    if (existing is not null) db.TeacherProfileValues.Remove(existing);
                    continue;
                }
                if (existing is null)
                {
                    existing = new TeacherProfileValue
                    {
                        TeacherProfileRowId = row.TeacherProfileRowId,
                        FacultyProfileFieldId = fieldId,
                    };
                    db.TeacherProfileValues.Add(existing);
                    existingValues.Add(existing);
                }
                existing.Value = cell!.Value!.Trim();
                existing.FileName = string.IsNullOrWhiteSpace(cell.FileName) ? null : cell.FileName.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (!partnerEditableOnly)
        {
            foreach (var row in existingRows.Where(x => !keptRowIds.Contains(x.TeacherProfileRowId)).ToList())
            {
                db.TeacherProfileValues.RemoveRange(
                    existingValues.Where(v => v.TeacherProfileRowId == row.TeacherProfileRowId));
                db.TeacherProfileRows.Remove(row);
            }
        }

        if (!string.IsNullOrWhiteSpace(body.DisplayName)) teacher.DisplayName = body.DisplayName.Trim();
        teacher.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await ApplySystemValuesAsync(db, teacherId, teacher.PartnerId, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Fills the system fields after a save.</summary>
    public static async Task ApplySystemValuesAsync(
        OdinDbContext db, Guid teacherId, Guid partnerId, CancellationToken ct)
    {
        var fields = await db.FacultyProfileFields
            .Where(f => f.DeletedAt == null)
            .ToListAsync(ct);
        var systemFields = fields.Where(f => IsSystemType(f.Type)).ToList();
        if (systemFields.Count == 0) return;

        var rows = await db.TeacherProfileRows
            .Where(r => r.TeacherId == teacherId && r.DeletedAt == null)
            .ToListAsync(ct);
        var rowIds = rows.Select(r => r.TeacherProfileRowId).ToList();
        var values = await db.TeacherProfileValues
            .Where(v => rowIds.Contains(v.TeacherProfileRowId))
            .ToListAsync(ct);

        var partnerName = await db.Partners
            .Where(p => p.PartnerId == partnerId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct) ?? "PARTNER";
        var partnerToken = new string(partnerName.Where(char.IsLetterOrDigit).ToArray());
        if (partnerToken.Length == 0) partnerToken = "PARTNER";

        var fieldsByLabel = fields
            .GroupBy(f => f.Label.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var field in systemFields)
        foreach (var row in rows.Where(r => r.FacultyProfileSectionId == field.FacultyProfileSectionId))
        {
            var cell = values.FirstOrDefault(v =>
                v.TeacherProfileRowId == row.TeacherProfileRowId
                && v.FacultyProfileFieldId == field.FacultyProfileFieldId);

            if (field.Type == FacultyProfileField.TypeAutoId)
            {
                if (!string.IsNullOrWhiteSpace(cell?.Value)) continue; // ids never change
                var next = await NextSequenceAsync(db, field.FacultyProfileFieldId, partnerId, ct);
                var id = (field.OptionsText ?? "{partner}-{n}")
                    .Replace("{partner}", partnerToken, StringComparison.OrdinalIgnoreCase)
                    .Replace("{n}", next.ToString("D3"), StringComparison.OrdinalIgnoreCase);
                Upsert(db, values, row.TeacherProfileRowId, field.FacultyProfileFieldId, cell, id);
            }
            else
            {
                var text = field.OptionsText ?? string.Empty;
                foreach (var (label, sibling) in fieldsByLabel)
                {
                    if (!text.Contains('{')) break;
                    var siblingValue = values.FirstOrDefault(v =>
                        v.TeacherProfileRowId == row.TeacherProfileRowId
                        && v.FacultyProfileFieldId == sibling.FacultyProfileFieldId)?.Value ?? string.Empty;
                    text = text.Replace("{" + label + "}", siblingValue, StringComparison.OrdinalIgnoreCase);
                }
                text = text.Trim();
                if (string.IsNullOrWhiteSpace(text) && cell is null) continue;
                Upsert(db, values, row.TeacherProfileRowId, field.FacultyProfileFieldId, cell, text);
            }
        }
    }

    private static async Task<int> NextSequenceAsync(
        OdinDbContext db, Guid fieldId, Guid partnerId, CancellationToken ct)
    {
        var issued = await (
            from v in db.TeacherProfileValues
            join r in db.TeacherProfileRows on v.TeacherProfileRowId equals r.TeacherProfileRowId
            join t in db.Teachers on r.TeacherId equals t.TeacherId
            where v.FacultyProfileFieldId == fieldId && t.PartnerId == partnerId && v.Value != ""
            select v.Value).ToListAsync(ct);
        var max = 0;
        foreach (var value in issued)
        {
            var tail = value.Split('-').LastOrDefault();
            if (int.TryParse(tail, out var n) && n > max) max = n;
        }
        return max + 1;
    }

    private static void Upsert(
        OdinDbContext db, List<TeacherProfileValue> values,
        Guid rowId, Guid fieldId, TeacherProfileValue? cell, string text)
    {
        if (cell is null)
        {
            cell = new TeacherProfileValue { TeacherProfileRowId = rowId, FacultyProfileFieldId = fieldId };
            db.TeacherProfileValues.Add(cell);
            values.Add(cell);
        }
        cell.Value = text;
        cell.UpdatedAt = DateTime.UtcNow;
    }
}
