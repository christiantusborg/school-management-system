using System.Text;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>CSV plumbing shared by the admin import endpoints
/// (student import and grade import).</summary>
internal static class ImportCsvHelpers
{
    internal static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    /// <summary>Minimal RFC 4180 parser: quoted fields, doubled quotes,
    /// CRLF or LF line ends. Returns one string[] per line.</summary>
    internal static List<string[]> ParseCsv(string text)
    {
        var rows = new List<string[]>();
        var current = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var any = false;

        text = text.TrimStart('\uFEFF');
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(ch);
                any = true;
            }
            else switch (ch)
            {
                case '"': inQuotes = true; any = true; break;
                case ',': current.Add(field.ToString()); field.Clear(); any = true; break;
                case '\r': break;
                case '\n':
                    if (any) { current.Add(field.ToString()); rows.Add(current.ToArray()); }
                    current = new List<string>(); field.Clear(); any = false;
                    break;
                default: field.Append(ch); any = true; break;
            }
        }
        if (any) { current.Add(field.ToString()); rows.Add(current.ToArray()); }
        return rows;
    }
}
