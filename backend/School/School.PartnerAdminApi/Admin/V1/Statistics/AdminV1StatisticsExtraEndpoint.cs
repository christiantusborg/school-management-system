using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SharedLibrary.Basics.Opaque.Domains;

namespace School.PartnerAdminApi.Admin.V1.Statistics;

/// <summary>
/// Dashboard → Statistics, the analytical tabs beyond Outcomes:
///  - grades: averages, distribution bands, pass/fail per module / programme /
///    partner / school + module difficulty ranking + rubric criterion averages
///  - teachers: marks given per cohort teacher, benchmarked as the deviation
///    from each module's GLOBAL average (fair even for hard modules)
///  - demographics: gender / age bands / nationality / industry / position,
///    overall and split per partner and programme
///  - operations: grading-sheet timeliness, QA completion, lead times and the
///    stalled-student early-warning list
///  - finance: paid vs overdue payment amounts and counts per partner
///  - trends: enrolments and average grade per month or quarter
/// </summary>
[Route("/v1/admin/statistics")]
[EndpointTag("Admin.Statistics")]
public sealed class AdminV1StatisticsExtraEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/statistics/grades", GradesAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/teachers", TeachersAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/demographics", DemographicsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/operations", OperationsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/finance", FinanceAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/trends", TrendsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/signups", SignupsAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/full-report", FullReportAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/statistics/{tab}/export", ExportTabAsync).RequireAuthorization("AdminOnly");
        // Ad-blocker-safe aliases: filter lists (EasyPrivacy etc.) block URLs
        // containing "statistics", killing browser download navigations. Same
        // handlers, neutral wording.
        app.MapGet("/v1/admin/overview/full/file", FullReportAsync).RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/overview/{tab}/file", ExportTabAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static readonly string[] ExcludedCodes =
    [
        "Draft", "ApplicationRejectedByPartner", "ApplicationRejectedByAdmission",
    ];

    private static (DateTime? From, DateTime? ToExclusive) Range(DateTime? from, DateTime? to) => (
        from is { } f ? DateTime.SpecifyKind(f.Date, DateTimeKind.Unspecified) : null,
        to is { } t ? DateTime.SpecifyKind(t.Date.AddDays(1), DateTimeKind.Unspecified) : null);

    // ── Grades ──────────────────────────────────────────────────────────────

    private sealed record GradeRow(int Score, string ModuleCode, string ModuleName,
        string Partner, string School, string Programme);

    private static async Task<List<GradeRow>> LoadGradesAsync(
        OdinDbContext db, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var (f, t) = Range(from, to);
        var rows = await (
            from g in db.SubjectGrades
            join sub in db.Subjects on g.SubjectId equals sub.SubjectId
            join e in db.Enrollments on g.StudentEnrollmentId equals e.StudentEnrollmentId
            join sp in db.Specializations on e.SpecializationId equals sp.SpecializationId
            join p in db.Programmes on sp.ProgrammeId equals p.ProgrammeId
            where e.DeletedAt == null
                && (f == null || g.GradedAt >= f)
                && (t == null || g.GradedAt < t)
            select new
            {
                g.Score,
                ModuleCode = sub.Code,
                ModuleName = sub.Name,
                Partner = db.Partners.Where(x => x.PartnerId == e.PartnerId).Select(x => x.Name).FirstOrDefault(),
                School = db.Schools.Where(x => x.SchoolId == p.SchoolId).Select(x => x.Name).FirstOrDefault(),
                Programme = (p.Code ?? "") + " — " + p.Name,
            }).ToListAsync(ct);
        return rows.Select(r => new GradeRow(r.Score, r.ModuleCode, r.ModuleName,
            r.Partner ?? "(no partner)", r.School ?? "(no school)", r.Programme)).ToList();
    }

    private static object GradeStat(string label, List<int> scores, string? sub = null)
    {
        var n = scores.Count;
        double Pct(int c) => n == 0 ? 0 : Math.Round(c * 100.0 / n, 1);
        return new
        {
            label,
            sub,
            count = n,
            avg = n == 0 ? 0 : Math.Round(scores.Average(), 1),
            passPct = Pct(scores.Count(s => s >= 40)),
            failPct = Pct(scores.Count(s => s < 40)),
            bands = new[]
            {
                Pct(scores.Count(s => s < 40)),
                Pct(scores.Count(s => s is >= 40 and < 60)),
                Pct(scores.Count(s => s is >= 60 and < 80)),
                Pct(scores.Count(s => s >= 80)),
            },
        };
    }

    private static async Task<IResult> GradesAsync(
        OdinDbContext db, CancellationToken ct, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var rows = await LoadGradesAsync(db, from, to, ct);

        List<object> By(Func<GradeRow, string> key, Func<IGrouping<string, GradeRow>, string?>? sub = null) => rows
            .GroupBy(key)
            .OrderBy(g => g.Key)
            .Select(g => GradeStat(g.Key, g.Select(x => x.Score).ToList(), sub?.Invoke(g)))
            .ToList();

        // Difficulty ranking: hardest modules (lowest average) first.
        var byModule = rows
            .GroupBy(r => r.ModuleCode)
            .Select(g => GradeStat(g.Key, g.Select(x => x.Score).ToList(), g.First().ModuleName))
            .OrderBy(m => ((dynamic)m).avg)
            .ToList();

        var (f, t) = Range(from, to);
        var criteria = await (
            from rs in db.SubjectGradeRubricScores
            join rr in db.RubricRows on rs.RubricRowId equals rr.RubricRowId
            join g in db.SubjectGrades on rs.SubjectGradeId equals g.SubjectGradeId
            join sub in db.Subjects on g.SubjectId equals sub.SubjectId
            where (f == null || g.GradedAt >= f) && (t == null || g.GradedAt < t)
            select new { Module = sub.Code, rr.Section, rs.Score }).ToListAsync(ct);
        var rubricCriteria = criteria
            .GroupBy(c => c.Module)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                module = g.Key,
                criteria = g.GroupBy(x => x.Section)
                    .Select(sg => new { section = sg.Key, count = sg.Count(), avg = Math.Round(sg.Average(x => x.Score), 1) })
                    .OrderBy(x => x.avg)
                    .ToList(),
            }).ToList();

        return Results.Ok(new
        {
            overall = GradeStat("All grades", rows.Select(r => r.Score).ToList()),
            byModule,
            byPartner = By(r => r.Partner),
            bySchool = By(r => r.School),
            byProgramme = By(r => r.Programme),
            rubricCriteria,
        });
    }

    // ── Teachers ────────────────────────────────────────────────────────────

    private static async Task<IResult> TeachersAsync(
        OdinDbContext db, CancellationToken ct, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (f, t) = Range(from, to);
        var rows = await (
            from c in db.ModuleCohorts
            where c.DeletedAt == null && c.TeacherId != null
            join sub in db.Subjects on c.SubjectId equals sub.SubjectId
            join mcs in db.ModuleCohortStudents on c.ModuleCohortId equals mcs.ModuleCohortId
            where mcs.DeletedAt == null
            join e in db.Enrollments on mcs.StudentEnrollmentId equals e.StudentEnrollmentId
            where e.DeletedAt == null
            select new
            {
                c.TeacherId,
                c.ModuleCohortId,
                TeacherName = db.Teachers.Where(x => x.TeacherId == c.TeacherId).Select(x => x.DisplayName).FirstOrDefault(),
                Partner = db.Partners.Where(x => x.PartnerId == c.PartnerId).Select(x => x.Name).FirstOrDefault(),
                ModuleCode = sub.Code,
                Score = (
                    from g in db.SubjectGrades
                    join s2 in db.Subjects on g.SubjectId equals s2.SubjectId
                    where g.StudentEnrollmentId == e.StudentEnrollmentId
                        && s2.SpecializationId == e.SpecializationId
                        && s2.Code == sub.Code
                        && (f == null || g.GradedAt >= f)
                        && (t == null || g.GradedAt < t)
                    select (int?)g.Score).FirstOrDefault(),
            }).ToListAsync(ct);

        // Benchmark: the global average per module code over the same period.
        var allGrades = await LoadGradesAsync(db, from, to, ct);
        var moduleAvg = allGrades
            .GroupBy(r => r.ModuleCode)
            .ToDictionary(g => g.Key, g => g.Average(x => (double)x.Score));

        var teachers = rows
            .GroupBy(r => r.TeacherId!.Value)
            .Select(g =>
            {
                var graded = g.Where(x => x.Score != null).ToList();
                var deviations = graded
                    .Where(x => moduleAvg.ContainsKey(x.ModuleCode))
                    .Select(x => x.Score!.Value - moduleAvg[x.ModuleCode])
                    .ToList();
                return new
                {
                    teacher = g.First().TeacherName ?? "(unnamed)",
                    partner = g.First().Partner ?? "—",
                    cohorts = g.Select(x => x.ModuleCohortId).Distinct().Count(),
                    students = g.Count(),
                    graded = graded.Count,
                    avg = graded.Count == 0 ? (double?)null : Math.Round(graded.Average(x => (double)x.Score!.Value), 1),
                    deviation = deviations.Count == 0 ? (double?)null : Math.Round(deviations.Average(), 1),
                    smallSample = graded.Count < 5,
                };
            })
            .OrderByDescending(x => x.deviation ?? double.MinValue)
            .ToList();

        return Results.Ok(new { teachers });
    }

    // ── Demographics ────────────────────────────────────────────────────────

    private static async Task<IResult> DemographicsAsync(
        OdinDbContext db, CancellationToken ct, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (f, t) = Range(from, to);
        var rows = await (
            from e in db.Enrollments
            join st in db.Students on e.StudentId equals st.StudentId
            join sp in db.Specializations on e.SpecializationId equals sp.SpecializationId
            join p in db.Programmes on sp.ProgrammeId equals p.ProgrammeId
            where e.DeletedAt == null
                && e.CommencementDate != null
                && !ExcludedCodes.Contains(e.Status.Code)
                && (f == null || e.CommencementDate >= f)
                && (t == null || e.CommencementDate < t)
            select new
            {
                Partner = db.Partners.Where(x => x.PartnerId == e.PartnerId).Select(x => x.Name).FirstOrDefault(),
                Programme = (p.Code ?? "") + " — " + p.Name,
                st.Gender,
                st.DateOfBirth,
                st.DisabilityDisclosure,
                Nationality = db.Nationalities.Where(n => n.NationalityId == st.NationalityId).Select(n => n.Name).FirstOrDefault(),
                Industry = db.EmploymentIndustries.Where(i => i.EmploymentIndustryId == st.CurrentEmploymentIndustryId).Select(i => i.Name).FirstOrDefault(),
                Position = db.PositionFunctions.Where(x => x.PositionFunctionId == st.CurrentPositionFunctionId).Select(x => x.Name).FirstOrDefault(),
            }).ToListAsync(ct);

        var today = DateTime.UtcNow.Date;
        string AgeBand(DateTime? dob)
        {
            if (dob is null) return "Unknown";
            var age = (int)((today - dob.Value.Date).TotalDays / 365.25);
            return age < 25 ? "Under 25" : age < 35 ? "25–34" : age < 45 ? "35–44" : "45+";
        }

        object Dimension(string key, string label, Func<dynamic, string> pick)
        {
            var labeled = rows.Select(r => new { Cat = pick(r), r.Partner, r.Programme }).ToList();
            // Top 12 categories; the tail collapses into "Other".
            var top = labeled.GroupBy(x => x.Cat).OrderByDescending(g => g.Count())
                .Select(g => g.Key).Take(12).ToHashSet();
            string Cat(string c) => top.Contains(c) ? c : "Other";
            var cats = labeled.GroupBy(x => Cat(x.Cat)).OrderByDescending(g => g.Count())
                .Select(g => new { label = g.Key, count = g.Count() }).ToList();
            List<object> Split(Func<dynamic, string?> group) => labeled
                .GroupBy(x => (string)(group(x) ?? "(none)"))
                .OrderBy(g => g.Key)
                .Select(g => (object)new
                {
                    group = g.Key,
                    total = g.Count(),
                    cats = g.GroupBy(x => Cat(x.Cat)).ToDictionary(cg => cg.Key, cg => cg.Count()),
                }).ToList();
            return new { key, label, cats, byPartner = Split(x => x.Partner), byProgramme = Split(x => x.Programme) };
        }

        var avgAge = rows.Where(r => r.DateOfBirth != null)
            .Select(r => (today - r.DateOfBirth!.Value.Date).TotalDays / 365.25).DefaultIfEmpty().Average();

        return Results.Ok(new
        {
            total = rows.Count,
            avgAge = Math.Round(avgAge, 1),
            dimensions = new[]
            {
                Dimension("gender", "Gender", r => string.IsNullOrWhiteSpace(r.Gender) ? "Unknown" : (string)r.Gender),
                Dimension("age", "Age band", r => AgeBand(r.DateOfBirth)),
                Dimension("nationality", "Nationality", r => (string?)r.Nationality ?? "Unknown"),
                Dimension("industry", "Employment industry", r => (string?)r.Industry ?? "Unknown"),
                Dimension("position", "Position function", r => (string?)r.Position ?? "Unknown"),
                Dimension("disability", "Disability / learning difference disclosed",
                    r => string.IsNullOrWhiteSpace(r.DisabilityDisclosure) ? "Not answered" : (string)r.DisabilityDisclosure),
            },
        });
    }

    // ── Operations & QA ─────────────────────────────────────────────────────

    private static async Task<IResult> OperationsAsync(
        OdinDbContext db, CancellationToken ct, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (f, t) = Range(from, to);
        var today = DateTime.UtcNow.Date;

        var cohorts = await db.ModuleCohorts
            .Where(c => c.DeletedAt == null
                && (f == null || c.StartDate >= f)
                && (t == null || c.StartDate < t))
            .Select(c => new
            {
                Partner = db.Partners.Where(x => x.PartnerId == c.PartnerId).Select(x => x.Name).FirstOrDefault(),
                c.EndDate,
                c.GradingSheetDueOverride,
                c.GradingSheetUploadedDate,
                c.DocQaChecked,
                c.GradeQaChecked,
                HasTeacher = c.TeacherId != null,
            }).ToListAsync(ct);

        string SheetStatus(DateTime? due, DateTime? uploaded) =>
            uploaded != null ? (due == null || uploaded <= due ? "onTime" : "late")
            : due != null && due < today ? "missing"
            : "notDueYet";

        var perPartner = cohorts
            .GroupBy(c => c.Partner ?? "(no partner)")
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var statuses = g.Select(c => SheetStatus(
                    c.GradingSheetDueOverride ?? c.EndDate?.AddMonths(1), c.GradingSheetUploadedDate)).ToList();
                double Pct(int n) => g.Count() == 0 ? 0 : Math.Round(n * 100.0 / g.Count(), 1);
                return new
                {
                    partner = g.Key,
                    cohorts = g.Count(),
                    onTime = statuses.Count(s => s == "onTime"),
                    late = statuses.Count(s => s == "late"),
                    missing = statuses.Count(s => s == "missing"),
                    notDueYet = statuses.Count(s => s == "notDueYet"),
                    docQaPct = Pct(g.Count(c => c.DocQaChecked)),
                    gradeQaPct = Pct(g.Count(c => c.GradeQaChecked)),
                    withoutTeacher = g.Count(c => !c.HasTeacher),
                };
            }).ToList();

        // Average days cohort end → grading sheet uploaded.
        var uploadLead = cohorts
            .Where(c => c.EndDate != null && c.GradingSheetUploadedDate != null)
            .Select(c => (c.GradingSheetUploadedDate!.Value - c.EndDate!.Value).TotalDays)
            .ToList();

        // Average days partner-submit → admission approval, from status notes,
        // filtered on the approval date.
        var noteRows = await db.EnrollmentStatusNotes
            .Where(n => n.StatusId == EnrollmentStatusIds.AwaitingGradesApproval
                || n.StatusId == EnrollmentStatusIds.GradesApproved)
            .Select(n => new { n.EnrollmentId, n.StatusId, n.CreatedAt })
            .ToListAsync(ct);
        var approvalLeads = noteRows
            .GroupBy(n => n.EnrollmentId)
            .Select(g =>
            {
                var approved = g.Where(x => x.StatusId == EnrollmentStatusIds.GradesApproved)
                    .OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                if (approved is null) return (double?)null;
                if (f != null && approved.CreatedAt < f) return null;
                if (t != null && approved.CreatedAt >= t) return null;
                var submitted = g.Where(x => x.StatusId == EnrollmentStatusIds.AwaitingGradesApproval
                        && x.CreatedAt <= approved.CreatedAt)
                    .OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                return submitted is null ? null : (approved.CreatedAt - submitted.CreatedAt).TotalDays;
            })
            .Where(d => d != null).Select(d => d!.Value).ToList();

        // Stalled students: still-active enrolments, commenced 4+ months ago,
        // not a single grade saved. Always evaluated against today.
        var cutoff = DateTime.SpecifyKind(today.AddMonths(-4), DateTimeKind.Unspecified);
        var finalCodes = new[] { "GradesApproved", "DroppedOut", "Deferred" };
        var stalled = await (
            from e in db.Enrollments
            where e.DeletedAt == null
                && e.CommencementDate != null && e.CommencementDate <= cutoff
                && !ExcludedCodes.Contains(e.Status.Code)
                && !finalCodes.Contains(e.Status.Code)
                && !db.SubjectGrades.Any(g => g.StudentEnrollmentId == e.StudentEnrollmentId)
            join sp in db.Specializations on e.SpecializationId equals sp.SpecializationId
            join p in db.Programmes on sp.ProgrammeId equals p.ProgrammeId
            select new
            {
                partner = db.Partners.Where(x => x.PartnerId == e.PartnerId).Select(x => x.Name).FirstOrDefault(),
                studentNumber = db.Students.Where(s => s.StudentId == e.StudentId).Select(s => s.StudentNumber).FirstOrDefault(),
                name = db.Students.Where(s => s.StudentId == e.StudentId)
                    .Select(s => db.UserProfiles.Where(pr => pr.UserId == s.UserId)
                        .Select(pr => (pr.FirstName ?? "") + " " + (pr.LastName ?? "")).FirstOrDefault()).FirstOrDefault(),
                programme = (p.Code ?? "") + " — " + p.Name,
                commencement = e.CommencementDate,
            }).OrderBy(x => x.commencement).Take(200).ToListAsync(ct);

        return Results.Ok(new
        {
            perPartner,
            uploadLeadAvgDays = uploadLead.Count == 0 ? (double?)null : Math.Round(uploadLead.Average(), 1),
            uploadLeadCount = uploadLead.Count,
            approvalLeadAvgDays = approvalLeads.Count == 0 ? (double?)null : Math.Round(approvalLeads.Average(), 1),
            approvalLeadCount = approvalLeads.Count,
            stalled,
            stalledMonths = 4,
        });
    }

    // ── Finance ─────────────────────────────────────────────────────────────

    private static async Task<IResult> FinanceAsync(
        OdinDbContext db, CancellationToken ct, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (f, t) = Range(from, to);
        var today = DateTime.UtcNow.Date;
        var rows = await (
            from pay in db.EnrollmentPayments
            join e in db.Enrollments on pay.StudentEnrollmentId equals e.StudentEnrollmentId
            where e.DeletedAt == null
                && (f == null || pay.PaymentDueDate >= f)
                && (t == null || pay.PaymentDueDate < t)
            select new
            {
                Partner = db.Partners.Where(x => x.PartnerId == e.PartnerId).Select(x => x.Name).FirstOrDefault(),
                pay.PaymentDueDate,
                pay.PaymentDueAmount,
                pay.PaymentDateAt,
                e.StudentEnrollmentId,
            }).ToListAsync(ct);

        object Shape(string label, IEnumerable<dynamic> g) => new
        {
            partner = label,
            paidCount = g.Count(x => x.PaymentDateAt != null),
            paidAmount = Math.Round((decimal)g.Where(x => x.PaymentDateAt != null).Sum(x => (decimal)x.PaymentDueAmount), 2),
            overdueCount = g.Count(x => x.PaymentDateAt == null && x.PaymentDueDate < today),
            overdueAmount = Math.Round((decimal)g.Where(x => x.PaymentDateAt == null && x.PaymentDueDate < today).Sum(x => (decimal)x.PaymentDueAmount), 2),
            upcomingCount = g.Count(x => x.PaymentDateAt == null && x.PaymentDueDate >= today),
            upcomingAmount = Math.Round((decimal)g.Where(x => x.PaymentDateAt == null && x.PaymentDueDate >= today).Sum(x => (decimal)x.PaymentDueAmount), 2),
            studentsWithOverdue = g.Where(x => x.PaymentDateAt == null && x.PaymentDueDate < today)
                .Select(x => (Guid)x.StudentEnrollmentId).Distinct().Count(),
        };

        return Results.Ok(new
        {
            perPartner = rows.GroupBy(r => r.Partner ?? "(no partner)").OrderBy(g => g.Key)
                .Select(g => Shape(g.Key, g)).ToList(),
            overall = Shape("All partners", rows),
        });
    }

    // ── Trends ──────────────────────────────────────────────────────────────

    private static async Task<IResult> TrendsAsync(
        OdinDbContext db, CancellationToken ct,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        [FromQuery] string? granularity = null)
    {
        var (f, t) = Range(from, to);
        var quarterly = string.Equals(granularity, "quarter", StringComparison.OrdinalIgnoreCase);
        string Period(DateTime d) => quarterly
            ? $"{d.Year}-Q{(d.Month - 1) / 3 + 1}"
            : $"{d.Year}-{d.Month:D2}";

        var enrolments = await db.Enrollments
            .Where(e => e.DeletedAt == null
                && e.CommencementDate != null
                && !ExcludedCodes.Contains(e.Status.Code)
                && (f == null || e.CommencementDate >= f)
                && (t == null || e.CommencementDate < t))
            .Select(e => e.CommencementDate!.Value)
            .ToListAsync(ct);

        var grades = await db.SubjectGrades
            .Where(g => g.GradedAt != null
                && (f == null || g.GradedAt >= f)
                && (t == null || g.GradedAt < t))
            .Select(g => new { g.GradedAt, g.Score })
            .ToListAsync(ct);

        var periods = enrolments.Select(Period)
            .Concat(grades.Select(g => Period(g.GradedAt!.Value)))
            .Distinct().OrderBy(p => p).ToList();

        var enrolByPeriod = enrolments.GroupBy(Period).ToDictionary(g => g.Key, g => g.Count());
        var gradesByPeriod = grades.GroupBy(g => Period(g.GradedAt!.Value))
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Avg = Math.Round(g.Average(x => (double)x.Score), 1) });

        return Results.Ok(new
        {
            granularity = quarterly ? "quarter" : "month",
            series = periods.Select(p => new
            {
                period = p,
                enrolments = enrolByPeriod.GetValueOrDefault(p),
                gradedCount = gradesByPeriod.TryGetValue(p, out var g) ? g.Count : 0,
                avgGrade = gradesByPeriod.TryGetValue(p, out var g2) ? (double?)g2.Avg : null,
            }).ToList(),
        });
    }

    // ── Generic CSV / PDF export for the analytics tabs ─────────────────────
    // Reuses each tab's JSON payload (via IValueHttpResult), flattens it to
    // titled tables, and renders them as CSV sections or a QuestPDF document.

    private sealed record ExportTable(string Title, string[] Headers, List<string[]> Rows);

    private static readonly Dictionary<string, string> TabTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["grades"] = "Grades", ["teachers"] = "Teacher grading behaviour",
        ["demographics"] = "Demographics", ["operations"] = "Operations & QA",
        ["finance"] = "Finance", ["trends"] = "Trends", ["signups"] = "Signups",
    };

    private static async Task<IResult> ExportTabAsync(
        string tab, OdinDbContext db, HttpContext httpContext, ILogger<AdminV1StatisticsExtraEndpoint> log, CancellationToken ct,
        [FromQuery] string? format = null,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        [FromQuery] string? granularity = null)
    {
        log.LogInformation("[stats-dl] tab export requested: tab={Tab} format={Format} from={From} to={To}", tab, format, from, to);
        if (string.Equals(tab, "outcomes", StringComparison.OrdinalIgnoreCase))
            return await AdminV1StatisticsEndpoint.ExportAsync(db, ct, format, from, to);
        if (!TabTitles.TryGetValue(tab, out var title))
        {
            log.LogWarning("[stats-dl] unknown tab {Tab}", tab);
            return Results.NotFound();
        }
        IResult inner = tab.ToLowerInvariant() switch
        {
            "grades" => await GradesAsync(db, ct, from, to),
            "teachers" => await TeachersAsync(db, ct, from, to),
            "demographics" => await DemographicsAsync(db, ct, from, to),
            "operations" => await OperationsAsync(db, ct, from, to),
            "finance" => await FinanceAsync(db, ct, from, to),
            "signups" => await SignupsAsync(db, httpContext, ct, from, to),
            _ => await TrendsAsync(db, ct, from, to, granularity),
        };
        if (inner is not IValueHttpResult { Value: { } payload }) return Results.NotFound();
        var root = JsonSerializer.SerializeToElement(payload);
        var tables = TablesFor(tab.ToLowerInvariant(), root);

        var period = (from, to) switch
        {
            (null, null) => "all time",
            ({ } f, null) => $"from {f:yyyy-MM-dd}",
            (null, { } t) => $"until {t:yyyy-MM-dd}",
            ({ } f, { } t) => $"{f:yyyy-MM-dd} to {t:yyyy-MM-dd}",
        };
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
            ? Results.File(TablesPdf(title, period, tables), "application/pdf", $"statistics-{tab}-{stamp}.pdf")
            : Results.File(TablesCsv(tables), "text/csv", $"statistics-{tab}-{stamp}.csv");
    }

    private static string S(JsonElement e, string prop)
    {
        if (!e.TryGetProperty(prop, out var v)) return "";
        return v.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => "",
            JsonValueKind.True => "yes",
            JsonValueKind.False => "no",
            JsonValueKind.String => v.GetString() ?? "",
            _ => v.ToString(),
        };
    }

    private static List<ExportTable> TablesFor(string tab, JsonElement root)
    {
        var tables = new List<ExportTable>();

        void GradeTable(string title, string nameHead, JsonElement arr)
        {
            var rows = arr.EnumerateArray().Select(r =>
            {
                var bands = r.GetProperty("bands").EnumerateArray().Select(b => b.ToString()).ToArray();
                return new[] { S(r, "label"), S(r, "sub"), S(r, "count"), S(r, "avg"), S(r, "passPct"), S(r, "failPct"),
                    bands[0], bands[1], bands[2], bands[3] };
            }).ToList();
            tables.Add(new ExportTable(title,
                [nameHead, "Name", "Grades", "Average", "Pass %", "Fail %", "0-39 %", "40-59 %", "60-79 %", "80-100 %"], rows));
        }

        switch (tab)
        {
            case "grades":
                var overallWrap = JsonSerializer.SerializeToElement(new[] { JsonSerializer.Deserialize<object>(root.GetProperty("overall").GetRawText()) });
                GradeTable("Overall", "", overallWrap);
                GradeTable("By partner", "Partner", root.GetProperty("byPartner"));
                GradeTable("By school", "School", root.GetProperty("bySchool"));
                GradeTable("By programme", "Programme", root.GetProperty("byProgramme"));
                GradeTable("Module difficulty ranking (hardest first)", "Module", root.GetProperty("byModule"));
                tables.Add(new ExportTable("Rubric criterion averages",
                    ["Module", "Section", "Scores", "Average"],
                    root.GetProperty("rubricCriteria").EnumerateArray()
                        .SelectMany(m => m.GetProperty("criteria").EnumerateArray()
                            .Select(c => new[] { S(m, "module"), S(c, "section"), S(c, "count"), S(c, "avg") }))
                        .ToList()));
                break;

            case "teachers":
                tables.Add(new ExportTable("Teachers",
                    ["Teacher", "Partner", "Cohorts", "Students", "Graded", "Average", "Vs module avg", "Small sample"],
                    root.GetProperty("teachers").EnumerateArray().Select(t => new[]
                    {
                        S(t, "teacher"), S(t, "partner"), S(t, "cohorts"), S(t, "students"),
                        S(t, "graded"), S(t, "avg"), S(t, "deviation"), S(t, "smallSample"),
                    }).ToList()));
                break;

            case "demographics":
                foreach (var d in root.GetProperty("dimensions").EnumerateArray())
                {
                    var label = S(d, "label");
                    var cats = d.GetProperty("cats").EnumerateArray().Select(c => S(c, "label")).ToArray();
                    tables.Add(new ExportTable($"{label} — overall", ["Category", "Count"],
                        d.GetProperty("cats").EnumerateArray().Select(c => new[] { S(c, "label"), S(c, "count") }).ToList()));
                    foreach (var (prop, head) in new[] { ("byPartner", "Partner"), ("byProgramme", "Programme") })
                        tables.Add(new ExportTable($"{label} — per {head.ToLowerInvariant()}",
                            new[] { head, "Total" }.Concat(cats).ToArray(),
                            d.GetProperty(prop).EnumerateArray().Select(row =>
                            {
                                var catCounts = row.GetProperty("cats");
                                return new[] { S(row, "group"), S(row, "total") }
                                    .Concat(cats.Select(c => S(catCounts, c) is { Length: > 0 } v ? v : "0")).ToArray();
                            }).ToList()));
                }
                break;

            case "operations":
                tables.Add(new ExportTable("Summary", ["Metric", "Value"],
                [
                    ["Cohort end → grading sheet uploaded, avg days", S(root, "uploadLeadAvgDays")],
                    ["Sheets measured", S(root, "uploadLeadCount")],
                    ["Partner submit → admission approval, avg days", S(root, "approvalLeadAvgDays")],
                    ["Approvals measured", S(root, "approvalLeadCount")],
                ]));
                tables.Add(new ExportTable("Per partner",
                    ["Partner", "Cohorts", "On time", "Late", "Missing", "Not due yet", "Doc QA %", "Grade QA %", "No teacher"],
                    root.GetProperty("perPartner").EnumerateArray().Select(p => new[]
                    {
                        S(p, "partner"), S(p, "cohorts"), S(p, "onTime"), S(p, "late"), S(p, "missing"),
                        S(p, "notDueYet"), S(p, "docQaPct"), S(p, "gradeQaPct"), S(p, "withoutTeacher"),
                    }).ToList()));
                tables.Add(new ExportTable($"Stalled students ({S(root, "stalledMonths")}+ months, no grades)",
                    ["Partner", "Student #", "Name", "Programme", "Commencement"],
                    root.GetProperty("stalled").EnumerateArray().Select(x => new[]
                    {
                        S(x, "partner"), S(x, "studentNumber"), S(x, "name"), S(x, "programme"),
                        S(x, "commencement") is { Length: >= 10 } c ? c[..10] : S(x, "commencement"),
                    }).ToList()));
                break;

            case "finance":
                string[] FinRow(JsonElement p) =>
                [
                    S(p, "partner"), S(p, "paidCount"), S(p, "paidAmount"), S(p, "overdueCount"),
                    S(p, "overdueAmount"), S(p, "upcomingCount"), S(p, "upcomingAmount"), S(p, "studentsWithOverdue"),
                ];
                var finRows = root.GetProperty("perPartner").EnumerateArray().Select(FinRow).ToList();
                finRows.Add(FinRow(root.GetProperty("overall")));
                tables.Add(new ExportTable("Payments per partner",
                    ["Partner", "Paid", "Paid amount", "Overdue", "Overdue amount", "Upcoming", "Upcoming amount", "Students overdue"],
                    finRows));
                break;

            case "signups":
                if (root.GetProperty("isSuper").GetBoolean())
                {
                    tables.Add(new ExportTable("Summary", ["Metric", "Value"],
                    [
                        ["Total signups", S(root, "total")],
                        ["Brought in by staff", S(root, "staffSignups")],
                        ["Self signups (public page)", S(root, "selfSignups")],
                    ]));
                    tables.Add(new ExportTable("Leaderboard",
                        ["Rank", "Who", "Office", "Signups"],
                        root.GetProperty("leaderboard").EnumerateArray().Select(r => new[]
                        {
                            S(r, "rank"), S(r, "name"), S(r, "office"), S(r, "count"),
                        }).ToList()));
                }
                else
                {
                    var me = root.GetProperty("me");
                    tables.Add(new ExportTable("Your signups", ["Metric", "Value"],
                    [
                        ["Signups you brought in", S(me, "count")],
                        ["Your rank", S(me, "rank")],
                        ["Ranked staff in period", S(me, "of")],
                    ]));
                }
                tables.Add(new ExportTable("Signups per day", ["Date", "Count"],
                    root.GetProperty("timeline").EnumerateArray()
                        .Select(d => new[] { S(d, "date"), S(d, "count") }).ToList()));
                break;

            case "trends":
                tables.Add(new ExportTable($"Trends ({S(root, "granularity")})",
                    ["Period", "Enrolments", "Grades saved", "Avg grade"],
                    root.GetProperty("series").EnumerateArray().Select(r => new[]
                    {
                        S(r, "period"), S(r, "enrolments"), S(r, "gradedCount"), S(r, "avgGrade"),
                    }).ToList()));
                break;
        }
        return tables;
    }

    private static byte[] TablesCsv(List<ExportTable> tables)
    {
        var sb = new StringBuilder();
        string E(string v) => v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
        foreach (var t in tables)
        {
            sb.AppendLine(E(t.Title));
            sb.AppendLine(string.Join(',', t.Headers.Select(E)));
            foreach (var row in t.Rows) sb.AppendLine(string.Join(',', row.Select(E)));
            sb.AppendLine();
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static byte[] TablesPdf(string title, string period, List<ExportTable> tables)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Header().Column(col =>
                {
                    col.Item().Text($"MGW — {title} Statistics").FontSize(16).Bold().FontColor("#003366");
                    col.Item().Text($"Period: {period} · generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(9).FontColor("#555555");
                    col.Item().PaddingTop(6);
                });

                page.Content().Column(col =>
                {
                    foreach (var t in tables)
                    {
                        if (t.Rows.Count == 0) continue;
                        col.Item().PaddingTop(8).Text(t.Title).FontSize(11).Bold().FontColor("#003366");
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                for (var i = 0; i < t.Headers.Length; i++)
                                    c.RelativeColumn(i == 0 ? 3 : 1.2f);
                            });
                            foreach (var h in t.Headers)
                                table.Cell().Background("#f0f3f7").Padding(3).Text(h).Bold().FontSize(7);
                            var odd = false;
                            foreach (var row in t.Rows)
                            {
                                odd = !odd;
                                foreach (var cell in row)
                                    table.Cell().Background(odd ? "#ffffff" : "#f8fafc").Padding(3).Text(cell).FontSize(7);
                            }
                        });
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Page ").FontSize(7);
                    x.CurrentPageNumber().FontSize(7);
                    x.Span(" of ").FontSize(7);
                    x.TotalPages().FontSize(7);
                });
            });
        }).GeneratePdf();
    }

    // ── Full report: every tab in one PDF or one multi-sheet XLSX workbook ──

    private static List<ExportTable> OutcomesTables(JsonElement root)
    {
        string[] headers = ["Group", "Programme", "Specialization", "Students",
            "Passed", "Passed %", "Dropped", "Dropped %", "Deferred", "Deferred %", "Active", "Active %"];
        string[] Row(string group, string prog, string spec, JsonElement b) =>
        [
            group, prog, spec, S(b, "total"), S(b, "passed"), S(b, "passedPct"), S(b, "dropped"),
            S(b, "droppedPct"), S(b, "deferred"), S(b, "deferredPct"), S(b, "active"), S(b, "activePct"),
        ];
        List<string[]> Tree(JsonElement nodes)
        {
            var rows = new List<string[]>();
            foreach (var n in nodes.EnumerateArray())
            {
                var label = S(n.GetProperty("bucket"), "label");
                rows.Add(Row(label, "", "", n.GetProperty("bucket")));
                foreach (var pg in n.GetProperty("programmes").EnumerateArray())
                {
                    rows.Add(Row(label, S(pg.GetProperty("programme"), "label"), "", pg.GetProperty("programme")));
                    foreach (var sp in pg.GetProperty("specializations").EnumerateArray())
                        rows.Add(Row(label, S(pg.GetProperty("programme"), "label"), S(sp, "label"), sp));
                }
            }
            return rows;
        }
        return
        [
            new ExportTable("Per partner", headers, Tree(root.GetProperty("byPartner"))),
            new ExportTable("Per school", headers, Tree(root.GetProperty("bySchool"))),
            new ExportTable("Overall", headers, [Row("All", "", "", root.GetProperty("overall"))]),
        ];
    }

    private static async Task<List<(string Title, List<ExportTable> Tables)>> BuildAllAsync(
        OdinDbContext db, DateTime? from, DateTime? to, string? granularity, CancellationToken ct)
    {
        var chapters = new List<(string, List<ExportTable>)>();
        var outcomes = await AdminV1StatisticsEndpoint.OutcomesAsync(db, ct, from, to);
        if (outcomes is IValueHttpResult { Value: { } op })
            chapters.Add(("Outcomes", OutcomesTables(JsonSerializer.SerializeToElement(op))));

        foreach (var tab in new[] { "grades", "teachers", "demographics", "operations", "finance", "trends" })
        {
            IResult inner = tab switch
            {
                "grades" => await GradesAsync(db, ct, from, to),
                "teachers" => await TeachersAsync(db, ct, from, to),
                "demographics" => await DemographicsAsync(db, ct, from, to),
                "operations" => await OperationsAsync(db, ct, from, to),
                "finance" => await FinanceAsync(db, ct, from, to),
                _ => await TrendsAsync(db, ct, from, to, granularity),
            };
            if (inner is IValueHttpResult { Value: { } payload })
                chapters.Add((TabTitles[tab], TablesFor(tab, JsonSerializer.SerializeToElement(payload))));
        }
        return chapters;
    }

    private static async Task<IResult> FullReportAsync(
        OdinDbContext db, ILogger<AdminV1StatisticsExtraEndpoint> log, CancellationToken ct,
        [FromQuery] string? format = null,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        [FromQuery] string? granularity = null)
    {
        log.LogInformation("[stats-dl] full report requested: format={Format} from={From} to={To}", format, from, to);
        var chapters = await BuildAllAsync(db, from, to, granularity, ct);
        log.LogInformation("[stats-dl] full report built: {Chapters} chapters", chapters.Count);
        var period = (from, to) switch
        {
            (null, null) => "all time",
            ({ } f, null) => $"from {f:yyyy-MM-dd}",
            (null, { } t) => $"until {t:yyyy-MM-dd}",
            ({ } f, { } t) => $"{f:yyyy-MM-dd} to {t:yyyy-MM-dd}",
        };
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd");
        return string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase)
            ? Results.File(FullXlsx(chapters, period),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"mgw-statistics-{stamp}.xlsx")
            : Results.File(FullPdf(chapters, period), "application/pdf", $"mgw-statistics-report-{stamp}.pdf");
    }

    private static byte[] FullXlsx(List<(string Title, List<ExportTable> Tables)> chapters, string period)
    {
        using var wb = new XLWorkbook();
        foreach (var (title, tables) in chapters)
        {
            // Sheet names: max 31 chars, no special chars.
            var sheetName = title.Replace("&", "and");
            if (sheetName.Length > 31) sheetName = sheetName[..31];
            var ws = wb.AddWorksheet(sheetName);
            var row = 1;
            ws.Cell(row, 1).Value = $"MGW Statistics — {title} · {period}";
            ws.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(13).Font.SetFontColor(XLColor.FromHtml("#003366"));
            row += 2;
            foreach (var t in tables)
            {
                ws.Cell(row, 1).Value = t.Title;
                ws.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(11).Font.SetFontColor(XLColor.FromHtml("#003366"));
                row++;
                for (var c = 0; c < t.Headers.Length; c++)
                {
                    var cell = ws.Cell(row, c + 1);
                    cell.Value = t.Headers[c];
                    cell.Style.Font.SetBold();
                    cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#eef2f8"));
                }
                row++;
                foreach (var r in t.Rows)
                {
                    for (var c = 0; c < r.Length; c++)
                    {
                        var cell = ws.Cell(row, c + 1);
                        // Real numbers become numeric cells so formulas/sorting work.
                        if (decimal.TryParse(r[c], System.Globalization.NumberStyles.Number,
                                System.Globalization.CultureInfo.InvariantCulture, out var num))
                            cell.Value = num;
                        else
                            cell.Value = r[c];
                    }
                    row++;
                }
                row++;
            }
            ws.Columns().AdjustToContents(1, Math.Min(row, 500));
            ws.SheetView.FreezeRows(1);
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] FullPdf(List<(string Title, List<ExportTable> Tables)> chapters, string period)
    {
        return Document.Create(doc =>
        {
            foreach (var (title, tables) in chapters)
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(x => x.FontSize(8));

                    page.Header().Column(col =>
                    {
                        col.Item().Text($"MGW Statistics Report — {title}").FontSize(15).Bold().FontColor("#003366");
                        col.Item().Text($"Period: {period} · generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                            .FontSize(8).FontColor("#555555");
                        col.Item().PaddingTop(4);
                    });

                    page.Content().Column(col =>
                    {
                        foreach (var t in tables)
                        {
                            if (t.Rows.Count == 0) continue;
                            col.Item().PaddingTop(8).Text(t.Title).FontSize(11).Bold().FontColor("#003366");
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    for (var i = 0; i < t.Headers.Length; i++)
                                        c.RelativeColumn(i == 0 ? 3 : 1.2f);
                                });
                                foreach (var h in t.Headers)
                                    table.Cell().Background("#f0f3f7").Padding(3).Text(h).Bold().FontSize(7);
                                var odd = false;
                                foreach (var r in t.Rows)
                                {
                                    odd = !odd;
                                    foreach (var cell in r)
                                        table.Cell().Background(odd ? "#ffffff" : "#f8fafc").Padding(3).Text(cell).FontSize(7);
                                }
                            });
                        }
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.Span($"{title} · Page ").FontSize(7);
                        x.CurrentPageNumber().FontSize(7);
                        x.Span(" of ").FontSize(7);
                        x.TotalPages().FontSize(7);
                    });
                });
            }
        }).GeneratePdf();
    }

    // ── Signup leaderboard ──────────────────────────────────────────────────
    // Who brought in the signups (Added by) in the period: SuperAdministrator
    // sees the full leaderboard; every other admin level (incl. Sales) sees
    // only their own numbers plus their anonymous rank.

    private static async Task<IResult> SignupsAsync(
        OdinDbContext db, HttpContext httpContext, CancellationToken ct,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        var (f, t) = Range(from, to);
        var callerId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var isSuper = httpContext.User.IsInRole("SuperAdministrator");

        var rows = await (
            from st in db.Students
            join u in db.Users on st.UserId equals u.Id
            where st.DeletedAt == null
                && (f == null || u.CreatedAt >= f)
                && (t == null || u.CreatedAt < t)
            select new { st.CreatedByUserId, u.CreatedAt }).ToListAsync(ct);

        var selfSignups = rows.Count(r => r.CreatedByUserId == null);
        var byCreator = rows.Where(r => r.CreatedByUserId != null)
            .GroupBy(r => r.CreatedByUserId!)
            .Select(g => new
            {
                Id = g.Key,
                Count = g.Count(),
                Days = g.GroupBy(x => x.CreatedAt.Date)
                    .ToDictionary(d => d.Key, d => d.Count()),
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        List<object> Timeline(IEnumerable<DateTime> dates)
        {
            return dates.GroupBy(d => d.Date).OrderBy(g => g.Key)
                .Select(g => (object)new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
                .ToList();
        }

        if (isSuper)
        {
            var creatorIds = byCreator.Select(x => x.Id).ToList();
            var names = creatorIds.Count == 0
                ? new Dictionary<string, (string Name, string Office)>()
                : (await db.Users
                    .Where(u => creatorIds.Contains(u.Id))
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.PartnerId,
                        Name = db.UserProfiles.Where(p => p.UserId == u.Id)
                            .Select(p => ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim())
                            .FirstOrDefault(),
                    })
                    .ToListAsync(ct))
                    .ToDictionary(
                        u => u.Id,
                        u => (Name: string.IsNullOrWhiteSpace(u.Name) ? (u.UserName ?? "?") : u.Name!,
                              Office: u.PartnerId != null ? "partner" : "admission"));

            return Results.Ok(new
            {
                isSuper = true,
                total = rows.Count,
                selfSignups,
                staffSignups = rows.Count - selfSignups,
                leaderboard = byCreator.Select((x, i) => new
                {
                    rank = i + 1,
                    name = names.TryGetValue(x.Id, out var n) ? n.Name : "?",
                    office = names.TryGetValue(x.Id, out var o) ? o.Office : "?",
                    count = x.Count,
                }).ToList(),
                timeline = Timeline(rows.Select(r => r.CreatedAt)),
            });
        }

        var mineIndex = byCreator.FindIndex(x => x.Id == callerId);
        var mine = mineIndex >= 0 ? byCreator[mineIndex] : null;
        return Results.Ok(new
        {
            isSuper = false,
            me = new
            {
                count = mine?.Count ?? 0,
                rank = mineIndex >= 0 ? mineIndex + 1 : (int?)null,
                of = byCreator.Count,
            },
            timeline = Timeline(rows
                .Where(r => r.CreatedByUserId == callerId)
                .Select(r => r.CreatedAt)),
        });
    }
}
