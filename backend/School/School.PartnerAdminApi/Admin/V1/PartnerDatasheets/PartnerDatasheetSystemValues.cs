using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace School.PartnerAdminApi.Admin.V1.PartnerDatasheets;

/// <summary>
/// Fills the system field types after a sheet save, on both the admin and
/// the partner save path. "autoid" cells get a one-time generated id from
/// the field's pattern ("MGW-ALC-FAC-{partner}-{n}"; {n} counts per
/// (field, partner), 3-digit padded; existing ids are never regenerated).
/// "computed" cells are always rebuilt from the row's sibling values via the
/// field's template ("{First name} {Last name}").
/// </summary>
public static class PartnerDatasheetSystemValues
{
    public static async Task ApplyAsync(
        OdinDbContext db, Guid sheetId, Guid partnerId, CancellationToken ct)
    {
        var sheet = await db.PartnerDatasheets
            .Where(s => s.PartnerDatasheetId == sheetId)
            .Select(s => new { s.PartnerDatasheetDefinitionId })
            .FirstOrDefaultAsync(ct);
        if (sheet is null) return;

        var sectionIds = await db.PartnerDatasheetSections
            .Where(s => s.PartnerDatasheetDefinitionId == sheet.PartnerDatasheetDefinitionId && s.DeletedAt == null)
            .Select(s => s.PartnerDatasheetSectionId)
            .ToListAsync(ct);
        var fields = await db.PartnerDatasheetFields
            .Where(f => sectionIds.Contains(f.PartnerDatasheetSectionId) && f.DeletedAt == null)
            .ToListAsync(ct);
        var systemFields = fields
            .Where(f => f.Type is PartnerDatasheetField.TypeAutoId or PartnerDatasheetField.TypeComputed)
            .ToList();
        if (systemFields.Count == 0) return;

        var rows = await db.PartnerDatasheetRows
            .Where(r => r.PartnerDatasheetId == sheetId && r.DeletedAt == null)
            .ToListAsync(ct);
        var rowIds = rows.Select(r => r.PartnerDatasheetRowId).ToList();
        var values = await db.PartnerDatasheetValues
            .Where(v => rowIds.Contains(v.PartnerDatasheetRowId))
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
        foreach (var row in rows.Where(r => r.PartnerDatasheetSectionId == field.PartnerDatasheetSectionId))
        {
            var cell = values.FirstOrDefault(v =>
                v.PartnerDatasheetRowId == row.PartnerDatasheetRowId
                && v.PartnerDatasheetFieldId == field.PartnerDatasheetFieldId);

            if (field.Type == PartnerDatasheetField.TypeAutoId)
            {
                if (!string.IsNullOrWhiteSpace(cell?.Value)) continue; // ids never change
                var next = await NextSequenceAsync(db, field.PartnerDatasheetFieldId, partnerId, ct);
                var id = (field.OptionsText ?? "{partner}-{n}")
                    .Replace("{partner}", partnerToken, StringComparison.OrdinalIgnoreCase)
                    .Replace("{n}", next.ToString("D3"), StringComparison.OrdinalIgnoreCase);
                Upsert(db, values, row.PartnerDatasheetRowId, field.PartnerDatasheetFieldId, cell, id);
            }
            else // computed
            {
                var template = field.OptionsText ?? string.Empty;
                var text = template;
                foreach (var (label, sibling) in fieldsByLabel)
                {
                    if (!text.Contains('{')) break;
                    var siblingValue = values.FirstOrDefault(v =>
                        v.PartnerDatasheetRowId == row.PartnerDatasheetRowId
                        && v.PartnerDatasheetFieldId == sibling.PartnerDatasheetFieldId)?.Value ?? string.Empty;
                    text = text.Replace("{" + label + "}", siblingValue, StringComparison.OrdinalIgnoreCase);
                }
                text = text.Trim();
                if (string.IsNullOrWhiteSpace(text) && cell is null) continue;
                Upsert(db, values, row.PartnerDatasheetRowId, field.PartnerDatasheetFieldId, cell, text);
            }
        }
    }

    /// <summary>Next {n} for this autoid field on this partner: one past the
    /// highest number already issued (never reuses a number after deletes).</summary>
    private static async Task<int> NextSequenceAsync(
        OdinDbContext db, Guid fieldId, Guid partnerId, CancellationToken ct)
    {
        var issued = await (
            from v in db.PartnerDatasheetValues
            join r in db.PartnerDatasheetRows on v.PartnerDatasheetRowId equals r.PartnerDatasheetRowId
            join s in db.PartnerDatasheets on r.PartnerDatasheetId equals s.PartnerDatasheetId
            where v.PartnerDatasheetFieldId == fieldId && s.PartnerId == partnerId && v.Value != ""
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
        OdinDbContext db, List<PartnerDatasheetValue> values,
        Guid rowId, Guid fieldId, PartnerDatasheetValue? cell, string text)
    {
        if (cell is null)
        {
            cell = new PartnerDatasheetValue { PartnerDatasheetRowId = rowId, PartnerDatasheetFieldId = fieldId };
            db.PartnerDatasheetValues.Add(cell);
            values.Add(cell);
        }
        cell.Value = text;
        cell.UpdatedAt = DateTime.UtcNow;
    }
}
