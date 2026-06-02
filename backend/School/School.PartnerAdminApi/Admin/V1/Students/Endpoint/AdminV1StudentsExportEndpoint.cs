using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Bulk-export students for Admission. Filters by partner + enrolment
/// status (ANY enrolment matches), picks fields per the request body,
/// and emits either a CSV or XLSX.
///
/// Whole response is streamed straight from memory — nothing is cached
/// on disk, so each click rebuilds from current data.
///
/// Also exposes /preview which returns just the row count so the modal
/// can show "Will export N students" without building the file.
/// </summary>
[Route("/v1/admin/students/export")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsExportEndpoint : IEndpointMarker
{
    public sealed class ExportRequest
    {
        public List<Guid>? PartnerIds { get; init; }
        public List<string>? StatusCodes { get; init; }
        public List<string>? Fields { get; init; }
        public string Format { get; init; } = "xlsx";
    }

    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/v1/admin/students/export", ExportAsync).RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/students/export/preview", PreviewAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> PreviewAsync(
        [FromBody] ExportRequest body, OdinDbContext db, CancellationToken ct)
    {
        var count = await BuildBaseQuery(db, body).CountAsync(ct);
        return Results.Ok(new { count });
    }

    private static async Task<IResult> ExportAsync(
        [FromBody] ExportRequest body, OdinDbContext db, CancellationToken ct)
    {
        var rows = await BuildExportRowsAsync(db, body, ct);
        var fields = (body.Fields ?? AllFieldIds().ToList()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var format = string.Equals(body.Format, "csv", StringComparison.OrdinalIgnoreCase) ? "csv" : "xlsx";
        byte[] dataBytes = format == "xlsx" ? BuildXlsx(rows, fields) : BuildCsv(rows, fields);
        var dataFilename = format == "xlsx" ? "students.xlsx" : "students.csv";
        var dataContentType = format == "xlsx"
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";

        return Results.File(dataBytes, dataContentType, dataFilename);
    }

    private static IQueryable<Student> BuildBaseQuery(OdinDbContext db, ExportRequest body)
    {
        IQueryable<Student> q = db.Students.Where(s => s.DeletedAt == null);

        if (body.PartnerIds is { Count: > 0 })
            q = q.Where(s => body.PartnerIds.Contains(s.PartnerId));

        if (body.StatusCodes is { Count: > 0 })
        {
            var codes = body.StatusCodes;
            q = q.Where(s => db.Enrollments
                .Any(e => e.StudentId == s.StudentId
                    && e.DeletedAt == null
                    && codes.Contains(e.Status.Code)));
        }

        return q;
    }

    private sealed record ExportRow(
        Guid StudentId,
        string? StudentNumber,
        string? Username,
        string? Email,
        bool EmailVerified,
        string? FirstName,
        string? LastName,
        DateTime? DateOfBirth,
        string? PassportId,
        string? NationalityName,
        string? AddressLine1,
        string? City,
        string? StateRegion,
        string? PostalCode,
        string? CountryName,
        string? HighestDegree,
        int? YearsWorkExperience,
        string? LanguagesJoined,
        string? EnrolmentsJoined,
        string? PartnerName);

    private static async Task<List<ExportRow>> BuildExportRowsAsync(
        OdinDbContext db, ExportRequest body, CancellationToken ct)
    {
        var studentIds = await BuildBaseQuery(db, body)
            .Select(s => s.StudentId)
            .ToListAsync(ct);

        if (studentIds.Count == 0) return new();

        var students = await db.Students
            .Where(s => studentIds.Contains(s.StudentId))
            .Select(s => new
            {
                s.StudentId,
                s.StudentNumber,
                s.UserId,
                s.PartnerId,
                s.DateOfBirth,
                s.PassportId,
                s.NationalityId,
                s.HighestDegree,
                s.YearsWorkExperience,
                User = new { s.User.UserName, s.User.Email, s.User.EmailConfirmed },
                Profile = db.UserProfiles.Where(p => p.UserId == s.UserId)
                    .Select(p => new { p.FirstName, p.LastName }).FirstOrDefault(),
                Address = db.UserAddresses.Where(a => a.UserId == s.UserId && a.IsPrimary)
                    .Select(a => new { a.Street, a.City, a.State, a.ZipCode, a.Country }).FirstOrDefault(),
                PartnerName = db.Partners.Where(p => p.PartnerId == s.PartnerId).Select(p => p.Name).FirstOrDefault(),
            })
            .ToListAsync(ct);

        var nationalityList = await db.Set<Nationality>()
            .Where(n => n.DeletedAt == null)
            .Select(n => new { n.NationalityId, n.Name, n.Code })
            .ToListAsync(ct);
        var nationalityMap = nationalityList.ToDictionary(n => n.NationalityId, n => new { n.Name, n.Code });

        var languages = await (
            from ul in db.UserLanguages
            join l in db.Set<Language>() on ul.LanguageId equals l.LanguageId
            where ul.DeletedAt == null
            select new { ul.UserId, LanguageName = l.Name, ul.Proficiency }
        ).ToListAsync(ct);
        var languagesByStudent = languages
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x =>
                $"{x.LanguageName} ({ProficiencyLabel(x.Proficiency)})").ToList());

        var enrolments = await db.Enrollments
            .Where(e => studentIds.Contains(e.StudentId) && e.DeletedAt == null)
            .Select(e => new
            {
                e.StudentId,
                ProgrammeCode = e.Specialization.Programmes.Code,
                SpecializationName = e.Specialization.Name,
                StatusName = e.Status.Name,
            })
            .ToListAsync(ct);
        var enrByStudent = enrolments
            .GroupBy(x => x.StudentId)
            .ToDictionary(g => g.Key,
                g => g.Select(e => $"{e.ProgrammeCode} - {e.SpecializationName} ({e.StatusName})").ToList());

        return students.Select(s =>
        {
            string? nationalityName = null;
            if (s.NationalityId is int nid && nationalityMap.TryGetValue(nid, out var nat))
                nationalityName = nat.Name;

            string? countryName = null;
            if (!string.IsNullOrWhiteSpace(s.Address?.Country))
            {
                // Country column stores the ISO-style code (e.g. "VN").
                var match = nationalityMap.Values.FirstOrDefault(n =>
                    string.Equals(n.Code, s.Address.Country, StringComparison.OrdinalIgnoreCase));
                countryName = match?.Name ?? s.Address.Country;
            }

            // The UserLanguages table keys on Student.StudentId per existing
            // codebase convention (see PartnerV1MyStudentsDetailEndpoint).
            // Both the GUID-shaped UserId column and StudentId resolve to the
            // same value for the student records we're exporting.
            var langStrings = languagesByStudent.GetValueOrDefault(s.StudentId);
            var langJoined = langStrings is null ? null : string.Join("; ", langStrings);

            var enrStrings = enrByStudent.GetValueOrDefault(s.StudentId);
            var enrJoined = enrStrings is null ? null : string.Join("; ", enrStrings);

            return new ExportRow(
                s.StudentId,
                s.StudentNumber,
                s.User.UserName,
                s.User.Email,
                s.User.EmailConfirmed,
                s.Profile?.FirstName,
                s.Profile?.LastName,
                s.DateOfBirth,
                s.PassportId,
                nationalityName,
                s.Address?.Street,
                s.Address?.City,
                s.Address?.State,
                s.Address?.ZipCode,
                countryName,
                s.HighestDegree,
                s.YearsWorkExperience,
                langJoined,
                enrJoined,
                s.PartnerName);
        }).ToList();
    }

    private static string ProficiencyLabel(LanguageProficiency p) => p switch
    {
        LanguageProficiency.Beginner     => "Beginner",
        LanguageProficiency.Intermediate => "Intermediate",
        LanguageProficiency.Fluent       => "Fluent",
        LanguageProficiency.Native       => "Native",
        _ => p.ToString(),
    };

    // Stable column order. The frontend sends a subset of these field IDs;
    // we render columns in the order below regardless of incoming order so
    // exports stay diffable across runs.
    private static readonly (string Id, string Header, Func<ExportRow, object?> Get)[] Columns = new (string, string, Func<ExportRow, object?>)[]
    {
        ("studentNumber",       "Student #",        r => r.StudentNumber),
        ("partnerName",         "Partner",          r => r.PartnerName),
        ("username",            "Username",         r => r.Username),
        ("email",               "Email",            r => r.Email),
        ("emailVerified",       "Email verified",   r => r.EmailVerified ? "Yes" : "No"),
        ("firstName",           "First name",       r => r.FirstName),
        ("lastName",            "Last name",        r => r.LastName),
        ("dateOfBirth",         "Date of birth",    r => r.DateOfBirth?.ToString("yyyy-MM-dd")),
        ("passportId",          "Passport / ID",    r => r.PassportId),
        ("nationalityName",     "Nationality",      r => r.NationalityName),
        ("addressLine1",        "Address",          r => r.AddressLine1),
        ("city",                "City",             r => r.City),
        ("stateRegion",         "State / Region",   r => r.StateRegion),
        ("postalCode",          "Postal code",      r => r.PostalCode),
        ("countryName",         "Country",          r => r.CountryName),
        ("highestDegree",       "Highest degree",   r => r.HighestDegree),
        ("yearsWorkExperience", "Years experience", r => r.YearsWorkExperience),
        ("languages",           "Languages",        r => r.LanguagesJoined),
        ("enrolments",          "Enrolments",       r => r.EnrolmentsJoined),
    };

    private static IEnumerable<string> AllFieldIds() => Columns.Select(c => c.Id);

    private static byte[] BuildXlsx(List<ExportRow> rows, HashSet<string> fields)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Students");
        var active = Columns.Where(c => fields.Contains(c.Id)).ToList();

        for (int i = 0; i < active.Count; i++)
            ws.Cell(1, i + 1).Value = active[i].Header;
        ws.Range(1, 1, 1, Math.Max(1, active.Count)).Style.Font.Bold = true;

        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < active.Count; c++)
            {
                var v = active[c].Get(rows[r]);
                ws.Cell(r + 2, c + 1).Value = v switch
                {
                    null => XLCellValue.FromObject(null),
                    int i => i,
                    long l => l,
                    bool b => b,
                    DateTime d => d,
                    _ => v.ToString() ?? "",
                };
            }
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] BuildCsv(List<ExportRow> rows, HashSet<string> fields)
    {
        var active = Columns.Where(c => fields.Contains(c.Id)).ToList();
        var sb = new StringBuilder();

        // UTF-8 BOM so Excel opens it without mojibake when the user
        // double-clicks the .csv on Windows.
        sb.Append('﻿');

        sb.AppendLine(string.Join(",", active.Select(c => CsvEscape(c.Header))));
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", active.Select(c => CsvEscape(c.Get(row)?.ToString() ?? ""))));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

}
