using System.Text;
using Odin.Api.Base.Letters;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Bulk CSV grade import for the Admission Office (admin Import tab, second
/// card). One CSV line = one grade: StudentNumber, ModuleCode (Subject.Code,
/// e.g. LXIIS.BIT.0011), Grade (0-100). The module is matched against the
/// student's enrolments via each enrolment's CURRENT specialization; a code
/// found in none (or in more than one) of the student's enrolments errors
/// the line instead of guessing.
///
/// Semantics mirror the admin draft grade save
/// (<see cref="AdminV1StudentsSaveGradesEndpoint"/>): existing scores are
/// overwritten, GradedAt is stamped, the enrolment status is NOT changed,
/// and released grade-bearing letters (transcripts/certificates) are
/// re-rendered best-effort so they never go stale.
///
/// Routes: GET  .../import/grades/sample     downloadable CSV template
///         POST .../import/grades/validate   dry run, nothing written
///         POST .../import/grades            performs the import
/// </summary>
[Route("/v1/admin/students/import/grades")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsGradeImportEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/import/grades/sample", SampleAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/students/import/grades/validate", ValidateAsync)
            .RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapPost("/v1/admin/students/import/grades", ImportAsync)
            .RequireAuthorization("AdminOnly").DisableAntiforgery();
        return app;
    }

    private static readonly string[] Columns = { "StudentNumber", "ModuleCode", "Grade" };

    private static async Task<IResult> SampleAsync(
        OdinDbContext db, CancellationToken ct, [FromQuery] Guid? partnerId = null)
    {
        // With a partner known, generate a real grading sheet instead of the
        // generic two-line example.
        if (partnerId is not null)
            return await PartnerSampleFileAsync(db, partnerId.Value, ct);
        return Sample();
    }

    /// <summary>
    /// Live grade sample for one partner: one row per (student, module of
    /// their current specialization) for every admitted/active enrolment,
    /// with the existing score pre-filled and a blank cell where no grade is
    /// stored yet — fill in the blanks and import.
    /// </summary>
    internal static async Task<IResult> PartnerSampleFileAsync(
        OdinDbContext db, Guid partnerId, CancellationToken ct)
    {
        var activeCodes = School.PartnerAdminApi.Admin.V1.ModuleCohorts.ModuleCohortLogic.AssignableStatusCodes;
        var rows = await (
            from e in db.Enrollments
            join st in db.Students on e.StudentId equals st.StudentId
            join sub in db.Subjects on e.SpecializationId equals sub.SpecializationId
            where e.DeletedAt == null && e.PartnerId == partnerId
                && st.DeletedAt == null && sub.DeletedAt == null
                && activeCodes.Contains(e.Status.Code)
            orderby st.StudentNumber, sub.Code
            select new
            {
                st.StudentNumber,
                ModuleCode = sub.Code,
                Score = db.SubjectGrades
                    .Where(g => g.StudentEnrollmentId == e.StudentEnrollmentId && g.SubjectId == sub.SubjectId)
                    .Select(g => (int?)g.Score).FirstOrDefault(),
            }).ToListAsync(ct);
        if (rows.Count == 0) return Sample();

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', Columns));
        foreach (var r in rows)
            sb.AppendLine($"{ImportCsvHelpers.CsvEscape(r.StudentNumber)},{ImportCsvHelpers.CsvEscape(r.ModuleCode)},{(r.Score?.ToString() ?? "")}");
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return Results.File(bytes, "text/csv", "grade-import-sample.csv");
    }

    internal static IResult Sample()
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', Columns));
        sb.AppendLine("ST-20260710-A11413,LXIIS.BIT.0011,45");
        sb.AppendLine("ST-20260710-A11413,LXIIS.BIT.0012,72");
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return Results.File(bytes, "text/csv", "grade-import-sample.csv");
    }

    private static async Task<IResult> ValidateAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (forced, fail) = ParsePartnerScope(httpContext);
        if (fail is not null) return fail;
        return await ValidateCoreAsync(db, httpContext.Request, forced, ct);
    }

    internal static async Task<IResult> ValidateCoreAsync(
        OdinDbContext db, HttpRequest request, Guid? forcedPartnerId, CancellationToken ct)
    {
        var (plans, fileError) = await BuildPlansAsync(db, request, forcedPartnerId, ct);
        if (fileError is not null) return Results.BadRequest(new { error = fileError });

        var rows = plans.Select(p => (object)new
        {
            row = p.RowNumber,
            studentNumber = p.StudentNumber,
            module = p.ModuleCode,
            moduleName = p.ModuleName,
            grade = p.GradeRaw,
            action = p.Errors.Count > 0 ? "error" : p.SkipReason is not null ? "skip"
                : p.ExistingScore is not null ? "update" : "add",
            message = p.Errors.Count > 0 ? string.Join(" ", p.Errors)
                : p.SkipReason
                ?? (p.ExistingScore is not null ? $"Replaces the current score of {p.ExistingScore}." : ""),
        }).ToList();

        return Results.Ok(Summarize(rows));
    }

    private static async Task<IResult> ImportAsync(
        HttpContext httpContext, OdinDbContext db,
        [FromServices] LetterReleaseService letterRelease, CancellationToken ct)
    {
        var (forced, fail) = ParsePartnerScope(httpContext);
        if (fail is not null) return fail;
        return await ImportCoreAsync(db, letterRelease, httpContext.Request, forced, ct);
    }

    internal static async Task<IResult> ImportCoreAsync(
        OdinDbContext db, LetterReleaseService letterRelease, HttpRequest request,
        Guid? forcedPartnerId, CancellationToken ct)
    {
        var (plans, fileError) = await BuildPlansAsync(db, request, forcedPartnerId, ct);
        if (fileError is not null) return Results.BadRequest(new { error = fileError });

        var now = DateTime.UtcNow;
        var touchedEnrolments = new HashSet<Guid>();
        var rows = new List<object>();

        foreach (var p in plans)
        {
            string action, message;
            if (p.Errors.Count > 0) { action = "error"; message = string.Join(" ", p.Errors); }
            else if (p.SkipReason is not null) { action = "skip"; message = p.SkipReason; }
            else
            {
                if (p.ExistingGrade is not null)
                {
                    action = "update";
                    message = $"Score {p.ExistingGrade.Score} replaced with {p.Score}.";
                    p.ExistingGrade.Score = p.Score;
                    p.ExistingGrade.GradedAt = now;
                }
                else
                {
                    action = "add";
                    message = $"Score {p.Score} recorded.";
                    db.SubjectGrades.Add(new SubjectGrade
                    {
                        SubjectGradeId = Guid.NewGuid(),
                        StudentEnrollmentId = p.EnrollmentId,
                        SubjectId = p.SubjectId,
                        Score = p.Score,
                        GradedAt = now,
                    });
                }
                touchedEnrolments.Add(p.EnrollmentId);
            }
            rows.Add(new
            {
                row = p.RowNumber,
                studentNumber = p.StudentNumber,
                module = p.ModuleCode,
                moduleName = p.ModuleName,
                grade = p.GradeRaw,
                action,
                message,
            });
        }

        await db.SaveChangesAsync(ct);

        // Same after-save duty as the draft grade save: released grade-bearing
        // letters must reflect the new scores. Best-effort, never fails the import.
        foreach (var enrollmentId in touchedEnrolments)
        {
            foreach (var (docTypeId, type) in new[]
            {
                (SystemDocumentTypeIds.Transcript, LetterType.Transcript),
                (SystemDocumentTypeIds.PrintableTranscript, LetterType.PrintableTranscript),
                (SystemDocumentTypeIds.Certificate, LetterType.Certificate),
                (SystemDocumentTypeIds.ProvisionalCertificate, LetterType.ProvisionalCertificate),
            })
            {
                var released = await db.StudentDocuments.AnyAsync(d =>
                    d.EnrollmentId == enrollmentId && d.DocumentTypeId == docTypeId && d.DeletedAt == null, ct);
                if (released)
                {
                    try { await letterRelease.ReleaseAsync(enrollmentId, type, ct); }
                    catch { /* keep the import result even if a re-render fails */ }
                }
            }
        }

        return Results.Ok(Summarize(rows));
    }

    private static object Summarize(List<object> rows)
    {
        string ActionOf(object r) => (string)r.GetType().GetProperty("action")!.GetValue(r)!;
        return new
        {
            total = rows.Count,
            added = rows.Count(r => ActionOf(r) == "add"),
            updated = rows.Count(r => ActionOf(r) == "update"),
            skipped = rows.Count(r => ActionOf(r) == "skip"),
            errors = rows.Count(r => ActionOf(r) == "error"),
            rows,
        };
    }

    private sealed class RowPlan
    {
        public int RowNumber;
        public readonly List<string> Errors = new();
        public string? SkipReason;
        public string? StudentNumber;
        public string? ModuleCode;
        public string? ModuleName;
        public string? GradeRaw;
        public int Score;
        public Guid EnrollmentId;
        public Guid SubjectId;
        public int? ExistingScore;
        public SubjectGrade? ExistingGrade;
    }

    /// <summary>?partnerId= on the admin routes scopes matching to that
    /// partner's students; the partner portal passes its own id directly.</summary>
    private static (Guid? PartnerId, IResult? Fail) ParsePartnerScope(HttpContext httpContext)
    {
        var raw = httpContext.Request.Query["partnerId"].ToString();
        if (string.IsNullOrEmpty(raw)) return (null, null);
        return Guid.TryParse(raw, out var pid)
            ? (pid, null)
            : (null, Results.BadRequest(new { error = "partnerId must be a GUID." }));
    }

    private static async Task<(List<RowPlan> Plans, string? FileError)> BuildPlansAsync(
        OdinDbContext db, HttpRequest request, Guid? forcedPartnerId, CancellationToken ct)
    {
        if (!request.HasFormContentType)
            return ([], "Upload the CSV as multipart form data (field name \"file\").");
        var form = await request.ReadFormAsync(ct);
        var file = form.Files["file"] ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            return ([], "No CSV file was uploaded.");

        string text;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            text = await reader.ReadToEndAsync(ct);

        var raw = ImportCsvHelpers.ParseCsv(text);
        if (raw.Count < 2)
            return ([], "The file has no data rows (expecting a header line plus at least one row).");

        var header = raw[0].Select(h => h.Trim()).ToArray();
        var known = Columns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = header.Where(h => h.Length > 0 && !known.Contains(h)).ToList();
        if (unknown.Count > 0)
            return ([], $"Unknown column(s): {string.Join(", ", unknown)}. Expected: {string.Join(", ", Columns)}.");
        foreach (var required in Columns)
            if (!header.Contains(required, StringComparer.OrdinalIgnoreCase))
                return ([], $"Missing required column: {required}.");

        var plans = new List<RowPlan>();
        for (var i = 1; i < raw.Count; i++)
        {
            if (raw[i].All(string.IsNullOrWhiteSpace)) continue;
            var plan = new RowPlan { RowNumber = i + 1 };
            string? Cell(string col)
            {
                var idx = Array.FindIndex(header, h => string.Equals(h, col, StringComparison.OrdinalIgnoreCase));
                var v = idx >= 0 && idx < raw[i].Length ? raw[i][idx].Trim() : "";
                return string.IsNullOrEmpty(v) ? null : v;
            }
            plan.StudentNumber = Cell("StudentNumber");
            plan.ModuleCode = Cell("ModuleCode");
            plan.GradeRaw = Cell("Grade");
            plans.Add(plan);
        }
        if (plans.Count == 0) return ([], "The file has no data rows.");

        // Preload everything the file touches.
        var numbers = plans.Select(p => p.StudentNumber).Where(n => n != null)
            .Distinct().Cast<string>().ToList();
        var studentsQuery = db.Students
            .Where(s => s.DeletedAt == null && numbers.Contains(s.StudentNumber));
        if (forcedPartnerId is { } scopedPartner)
            studentsQuery = studentsQuery.Where(s => s.PartnerId == scopedPartner);
        var students = await studentsQuery
            .Select(s => new { s.StudentId, s.StudentNumber })
            .ToListAsync(ct);
        var studentsByNumber = students.ToDictionary(s => s.StudentNumber, s => s, StringComparer.OrdinalIgnoreCase);

        var studentIds = students.Select(s => s.StudentId).ToList();
        var enrolments = await db.Enrollments
            .Where(e => e.DeletedAt == null && studentIds.Contains(e.StudentId))
            .Select(e => new { e.StudentEnrollmentId, e.StudentId, e.SpecializationId })
            .ToListAsync(ct);

        var specIds = enrolments.Select(e => e.SpecializationId).Distinct().ToList();
        var subjects = await db.Subjects
            .Where(s => s.DeletedAt == null && specIds.Contains(s.SpecializationId))
            .Select(s => new { s.SubjectId, s.SpecializationId, s.Code, s.Name })
            .ToListAsync(ct);

        var enrolmentIds = enrolments.Select(e => e.StudentEnrollmentId).ToList();
        var existingGrades = await db.SubjectGrades
            .Where(g => enrolmentIds.Contains(g.StudentEnrollmentId))
            .ToListAsync(ct);
        var gradesByKey = existingGrades.ToDictionary(g => (g.StudentEnrollmentId, g.SubjectId));

        var seenInFile = new HashSet<(Guid EnrollmentId, Guid SubjectId)>();

        foreach (var plan in plans)
        {
            if (plan.StudentNumber is null) plan.Errors.Add("StudentNumber is required.");
            if (plan.ModuleCode is null) plan.Errors.Add("ModuleCode is required.");
            if (plan.GradeRaw is null) plan.Errors.Add("Grade is required.");
            else if (!int.TryParse(plan.GradeRaw, out var score) || score < 0 || score > 100)
                plan.Errors.Add($"Grade '{plan.GradeRaw}' must be a whole number between 0 and 100.");
            else plan.Score = score;

            if (plan.StudentNumber is not null && !studentsByNumber.ContainsKey(plan.StudentNumber))
                plan.Errors.Add($"Unknown StudentNumber '{plan.StudentNumber}'.");

            if (plan.Errors.Count > 0) continue;

            var student = studentsByNumber[plan.StudentNumber!];
            var matches = enrolments
                .Where(e => e.StudentId == student.StudentId)
                .SelectMany(e => subjects
                    .Where(s => s.SpecializationId == e.SpecializationId
                        && string.Equals(s.Code, plan.ModuleCode, StringComparison.OrdinalIgnoreCase))
                    .Select(s => (Enrolment: e, Subject: s)))
                .ToList();

            if (matches.Count == 0)
            {
                plan.Errors.Add($"Student '{plan.StudentNumber}' has no enrolment containing module '{plan.ModuleCode}'.");
                continue;
            }
            if (matches.Count > 1)
            {
                plan.Errors.Add($"Module '{plan.ModuleCode}' exists in more than one of the student's " +
                    "enrolments, so the grade is ambiguous. Enter it from the student's grade sheet instead.");
                continue;
            }

            plan.EnrollmentId = matches[0].Enrolment.StudentEnrollmentId;
            plan.SubjectId = matches[0].Subject.SubjectId;
            plan.ModuleName = matches[0].Subject.Name;
            if (gradesByKey.TryGetValue((plan.EnrollmentId, plan.SubjectId), out var existing))
            {
                plan.ExistingGrade = existing;
                plan.ExistingScore = existing.Score;
            }

            if (!seenInFile.Add((plan.EnrollmentId, plan.SubjectId)))
                plan.SkipReason = "Duplicate line: an earlier line already grades this module for this student.";
        }

        return (plans, null);
    }
}
