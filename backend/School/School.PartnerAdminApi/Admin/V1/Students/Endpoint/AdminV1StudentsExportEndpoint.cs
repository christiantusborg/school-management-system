using System.Globalization;
using System.Text;
using ClosedXML.Excel;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Bulk-export students for Admission. Filters by partner + enrolment
/// status (ANY enrolment matches), picks fields per the request body,
/// and emits either a CSV or XLSX.
///
/// Output shape:
///   - No per-enrolment fields selected → one row per student.
///   - Any per-enrolment field selected → one row per (student × enrolment).
///     Student fields are duplicated across the enrolment rows; students
///     with no enrolments still emit one empty-enrolment row.
///
/// Whole response is streamed straight from memory — nothing is cached
/// on disk, so each click rebuilds from current data.
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
        app.MapPost("/v1/admin/students/export/sample", SampleAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> PreviewAsync(
        [FromBody] ExportRequest body, OdinDbContext db, CancellationToken ct)
    {
        var count = await BuildBaseQuery(db, body).CountAsync(ct);
        return Results.Ok(new { count });
    }

    // First N students worth of rows rendered exactly as the spreadsheet
    // would see them. When the user has selected per-enrolment fields the
    // output expands to one row per (student × enrolment), so the table
    // returned here may have more than N entries — that's intentional, it
    // mirrors what the full file will look like.
    private const int SampleStudentLimit = 10;

    private static async Task<IResult> SampleAsync(
        [FromBody] ExportRequest body, OdinDbContext db, CancellationToken ct)
    {
        var count = await BuildBaseQuery(db, body).CountAsync(ct);
        var rows = await BuildExportRowsAsync(db, body, ct, limit: SampleStudentLimit);
        var fields = (body.Fields ?? AllFieldIds().ToList()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var active = Columns.Where(c => fields.Contains(c.Id)).ToList();
        var perEnrolment = active.Any(c => c.IsEnrolmentLevel);

        var sampleRows = new List<Dictionary<string, object?>>();
        foreach (var r in rows)
        {
            var enrs = perEnrolment
                ? (r.Enrolments.Count > 0 ? r.Enrolments : new() { null! })
                : new() { (ExportEnrolment?)null! };
            foreach (var e in enrs)
            {
                var dict = new Dictionary<string, object?>(active.Count);
                foreach (var col in active)
                    dict[col.Id] = col.Get(r, e);
                sampleRows.Add(dict);
            }
        }

        return Results.Ok(new
        {
            count,
            columns = active.Select(c => new { id = c.Id, header = c.Header }).ToList(),
            rows = sampleRows,
        });
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

    private sealed record ExportEnrolment(
        Guid EnrollmentId,
        string? ProgrammeCode,
        string? ProgrammeName,
        string? SpecializationName,
        string? ModeOfStudy,
        string? StatusCode,
        string? StatusName,
        DateTime? CommencementDate,
        int? DurationMonths,
        DateTime? ApplicationDate,
        DateTime? ApprovedDate,
        DateTime? GraduatedDate,
        DateTime? OfferLetterDate,
        DateTime? AdmissionLetterDate,
        DateTime? TranscriptDate,
        DateTime? CertificateDate);

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
        string? PartnerName,
        List<ExportEnrolment> Enrolments);

    private static async Task<List<ExportRow>> BuildExportRowsAsync(
        OdinDbContext db, ExportRequest body, CancellationToken ct, int? limit = null)
    {
        var idQuery = BuildBaseQuery(db, body).Select(s => s.StudentId);
        if (limit is int n) idQuery = idQuery.Take(n);
        var studentIds = await idQuery.ToListAsync(ct);

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

        var enrolmentRaw = await db.Enrollments
            .Where(e => studentIds.Contains(e.StudentId) && e.DeletedAt == null)
            .Select(e => new
            {
                e.StudentEnrollmentId,
                e.StudentId,
                ProgrammeCode = e.Specialization.Programmes.Code,
                ProgrammeName = e.Specialization.Programmes.Name,
                SpecializationName = e.Specialization.Name,
                ModeOfStudyName = e.ModeOfStudy.Name,
                StatusCode = e.Status.Code,
                StatusName = e.Status.Name,
                StatusLevel = e.Status.Level,
                e.CommencementDate,
                e.Specialization.DurationOfStudyMonths,
            })
            .ToListAsync(ct);

        // Per-enrolment date columns are sourced from the status-note
        // timeline and the released-letter document rows. Application date
        // is "first time this enrolment moved past Draft" (min); approved /
        // graduated dates use the max so a re-bounce reports its latest
        // transition. Letter dates use the most recent uploaded letter doc.
        var enrolmentIds = enrolmentRaw.Select(e => e.StudentEnrollmentId).ToList();

        var noteAgg = enrolmentIds.Count == 0
            ? new()
            : await db.Set<EnrollmentStatusNote>()
                .Where(n => enrolmentIds.Contains(n.EnrollmentId))
                .GroupBy(n => new { n.EnrollmentId, n.StatusId })
                .Select(g => new
                {
                    g.Key.EnrollmentId,
                    g.Key.StatusId,
                    MinAt = g.Min(x => x.CreatedAt),
                    MaxAt = g.Max(x => x.CreatedAt),
                })
                .ToListAsync(ct);

        DateTime? FirstAt(Guid enrolmentId, Guid statusId) =>
            noteAgg.FirstOrDefault(n => n.EnrollmentId == enrolmentId && n.StatusId == statusId)?.MinAt;
        DateTime? LastAt(Guid enrolmentId, Guid statusId) =>
            noteAgg.FirstOrDefault(n => n.EnrollmentId == enrolmentId && n.StatusId == statusId)?.MaxAt;

        var letterTypes = new[]
        {
            SystemDocumentTypeIds.OfferLetter,
            SystemDocumentTypeIds.AdmissionLetter,
            SystemDocumentTypeIds.Transcript,
            SystemDocumentTypeIds.Certificate,
        };
        var letterAgg = enrolmentIds.Count == 0
            ? new()
            : await db.StudentDocuments
                .Where(d => d.EnrollmentId != null
                    && enrolmentIds.Contains(d.EnrollmentId!.Value)
                    && d.DeletedAt == null
                    && letterTypes.Contains(d.DocumentTypeId))
                .GroupBy(d => new { d.EnrollmentId, d.DocumentTypeId })
                .Select(g => new
                {
                    EnrollmentId = g.Key.EnrollmentId!.Value,
                    g.Key.DocumentTypeId,
                    MaxAt = g.Max(x => x.UploadedAt),
                })
                .ToListAsync(ct);
        DateTime? LetterAt(Guid enrolmentId, Guid docTypeId) =>
            letterAgg.FirstOrDefault(d => d.EnrollmentId == enrolmentId && d.DocumentTypeId == docTypeId)?.MaxAt;

        var enrolmentsByStudent = enrolmentRaw
            .GroupBy(e => e.StudentId)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(e => e.CommencementDate ?? DateTime.MaxValue)
                .ThenBy(e => e.ProgrammeCode)
                .Select(e => new ExportEnrolment(
                    e.StudentEnrollmentId,
                    e.ProgrammeCode,
                    e.ProgrammeName,
                    e.SpecializationName,
                    e.ModeOfStudyName,
                    e.StatusCode,
                    e.StatusName,
                    e.CommencementDate,
                    e.DurationOfStudyMonths,
                    FirstAt(e.StudentEnrollmentId, EnrollmentStatusIds.ApplicationSubmitted),
                    LastAt(e.StudentEnrollmentId, EnrollmentStatusIds.ApplicationApprovedAdmission),
                    LastAt(e.StudentEnrollmentId, EnrollmentStatusIds.GradesApproved),
                    LetterAt(e.StudentEnrollmentId, SystemDocumentTypeIds.OfferLetter),
                    LetterAt(e.StudentEnrollmentId, SystemDocumentTypeIds.AdmissionLetter),
                    LetterAt(e.StudentEnrollmentId, SystemDocumentTypeIds.Transcript),
                    LetterAt(e.StudentEnrollmentId, SystemDocumentTypeIds.Certificate)))
                .ToList());

        return students.Select(s =>
        {
            string? nationalityName = null;
            if (s.NationalityId is int nid && nationalityMap.TryGetValue(nid, out var nat))
                nationalityName = nat.Name;

            string? countryName = null;
            if (!string.IsNullOrWhiteSpace(s.Address?.Country))
            {
                var match = nationalityMap.Values.FirstOrDefault(n =>
                    string.Equals(n.Code, s.Address.Country, StringComparison.OrdinalIgnoreCase));
                countryName = match?.Name ?? s.Address.Country;
            }

            var langStrings = languagesByStudent.GetValueOrDefault(s.StudentId);
            var langJoined = langStrings is null ? null : string.Join("; ", langStrings);

            var enrs = enrolmentsByStudent.GetValueOrDefault(s.StudentId) ?? new();
            string? enrJoined = enrs.Count == 0
                ? null
                : string.Join("; ", enrs.Select(e =>
                    $"{e.ProgrammeCode} - {e.SpecializationName} ({e.StatusName})"));

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
                s.PartnerName,
                enrs);
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

    private sealed record Column(string Id, string Header, bool IsEnrolmentLevel, Func<ExportRow, ExportEnrolment?, object?> Get);

    private static string? Iso(DateTime? d) => d?.ToString("yyyy-MM-dd");

    // Stable column order. The frontend sends a subset of these field IDs;
    // we render columns in the order below regardless of incoming order so
    // exports stay diffable across runs.
    private static readonly Column[] Columns = new Column[]
    {
        new("studentNumber",       "Student #",        false, (r, _) => r.StudentNumber),
        new("partnerName",         "Partner",          false, (r, _) => r.PartnerName),
        new("username",            "Username",         false, (r, _) => r.Username),
        new("email",               "Email",            false, (r, _) => r.Email),
        new("emailVerified",       "Email verified",   false, (r, _) => r.EmailVerified ? "Yes" : "No"),
        new("firstName",           "First name",       false, (r, _) => r.FirstName),
        new("lastName",            "Last name",        false, (r, _) => r.LastName),
        new("dateOfBirth",         "Date of birth",    false, (r, _) => Iso(r.DateOfBirth)),
        new("passportId",          "Passport / ID",    false, (r, _) => r.PassportId),
        new("nationalityName",     "Nationality",      false, (r, _) => r.NationalityName),
        new("addressLine1",        "Address",          false, (r, _) => r.AddressLine1),
        new("city",                "City",             false, (r, _) => r.City),
        new("stateRegion",         "State / Region",   false, (r, _) => r.StateRegion),
        new("postalCode",          "Postal code",      false, (r, _) => r.PostalCode),
        new("countryName",         "Country",          false, (r, _) => r.CountryName),
        new("highestDegree",       "Highest degree",   false, (r, _) => r.HighestDegree),
        new("yearsWorkExperience", "Years experience", false, (r, _) => r.YearsWorkExperience),
        new("languages",           "Languages",        false, (r, _) => r.LanguagesJoined),
        new("enrolments",          "Enrolments",       false, (r, _) => r.EnrolmentsJoined),
        new("programmeCode",       "Programme code",   true,  (_, e) => e?.ProgrammeCode),
        new("programmeName",       "Programme",        true,  (_, e) => e?.ProgrammeName),
        new("specializationName",  "Specialisation",   true,  (_, e) => e?.SpecializationName),
        new("modeOfStudy",         "Mode of study",    true,  (_, e) => e?.ModeOfStudy),
        new("statusCode",          "Status code",      true,  (_, e) => e?.StatusCode),
        new("statusName",          "Status",           true,  (_, e) => e?.StatusName),
        new("commencementDate",    "Start date",       true,  (_, e) => Iso(e?.CommencementDate)),
        new("durationMonths",      "Duration (months)",true,  (_, e) => e?.DurationMonths),
        new("applicationDate",     "Application date", true,  (_, e) => Iso(e?.ApplicationDate)),
        new("approvedDate",        "Approved date",    true,  (_, e) => Iso(e?.ApprovedDate)),
        new("graduatedDate",       "Graduated date",   true,  (_, e) => Iso(e?.GraduatedDate)),
        new("offerLetterDate",     "Offer letter date",true,  (_, e) => Iso(e?.OfferLetterDate)),
        new("admissionLetterDate", "Admission letter date", true, (_, e) => Iso(e?.AdmissionLetterDate)),
        new("transcriptDate",      "Transcript date",  true,  (_, e) => Iso(e?.TranscriptDate)),
        new("certificateDate",     "Certificate date", true,  (_, e) => Iso(e?.CertificateDate)),
    };

    private static IEnumerable<string> AllFieldIds() => Columns.Select(c => c.Id);

    // Enumerate output rows respecting the per-enrolment expansion rule.
    // Per-enrolment selected + no enrolments → emit one row with null
    // enrolment so the student still shows up in the export.
    private static IEnumerable<(ExportRow Row, ExportEnrolment? Enr)> Expand(List<ExportRow> rows, bool perEnrolment)
    {
        foreach (var r in rows)
        {
            if (!perEnrolment) { yield return (r, null); continue; }
            if (r.Enrolments.Count == 0) { yield return (r, null); continue; }
            foreach (var e in r.Enrolments) yield return (r, e);
        }
    }

    private static byte[] BuildXlsx(List<ExportRow> rows, HashSet<string> fields)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Students");
        var active = Columns.Where(c => fields.Contains(c.Id)).ToList();
        var perEnrolment = active.Any(c => c.IsEnrolmentLevel);

        for (int i = 0; i < active.Count; i++)
            ws.Cell(1, i + 1).Value = active[i].Header;
        ws.Range(1, 1, 1, Math.Max(1, active.Count)).Style.Font.Bold = true;

        int outRow = 2;
        foreach (var (r, e) in Expand(rows, perEnrolment))
        {
            for (int c = 0; c < active.Count; c++)
            {
                var v = active[c].Get(r, e);
                ws.Cell(outRow, c + 1).Value = v switch
                {
                    null => XLCellValue.FromObject(null),
                    int i => i,
                    long l => l,
                    bool b => b,
                    DateTime d => d,
                    _ => v.ToString() ?? "",
                };
            }
            outRow++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] BuildCsv(List<ExportRow> rows, HashSet<string> fields)
    {
        var active = Columns.Where(c => fields.Contains(c.Id)).ToList();
        var perEnrolment = active.Any(c => c.IsEnrolmentLevel);
        var sb = new StringBuilder();

        // UTF-8 BOM so Excel opens it without mojibake on a double-click.
        sb.Append('﻿');

        sb.AppendLine(string.Join(",", active.Select(c => CsvEscape(c.Header))));
        foreach (var (r, e) in Expand(rows, perEnrolment))
        {
            sb.AppendLine(string.Join(",", active.Select(c => CsvEscape(c.Get(r, e)?.ToString() ?? ""))));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string CsvEscape(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
