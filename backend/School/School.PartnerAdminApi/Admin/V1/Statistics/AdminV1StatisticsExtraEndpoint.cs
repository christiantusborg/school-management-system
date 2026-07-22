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
}
