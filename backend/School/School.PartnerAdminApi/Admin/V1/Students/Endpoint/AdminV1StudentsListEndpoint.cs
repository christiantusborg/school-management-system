namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Admin students list — drives the Admin Dashboard → Students tab.
/// Returns enrollments grouped by student plus per-status counts the UI shows.
///
/// Each enrolment carries:
///   statusCode — authoritative <see cref="EnrollmentStatus.Code"/> value
///   statusName — human label from <see cref="EnrollmentStatus.Name"/>
/// The frontend filters and styles purely off statusCode; there is no
/// synthetic integer id on the wire.
/// </summary>
[Route("/v1/admin/students")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students", HandleAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        OdinDbContext db,
        HttpContext httpContext,
        Odin.Api.Base.Authorization.IPermissionService perms,
        CancellationToken cancellationToken,
        [FromQuery] Guid? partnerId = null,
        [FromQuery] Guid? programmeId = null,
        [FromQuery] Guid? specializationId = null,
        [FromQuery] DateTime? commencementFrom = null,
        [FromQuery] DateTime? commencementTo = null,
        [FromQuery] DateTime? graduationFrom = null,
        [FromQuery] DateTime? graduationTo = null)
    {
        // Pull every (non-deleted) enrollment + projected status.
        var enrollmentsQuery = db.Enrollments
            .Where(e => e.DeletedAt == null);
        if (partnerId is not null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.PartnerId == partnerId);
        if (programmeId is not null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.Specialization.ProgrammeId == programmeId);
        if (specializationId is not null)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.SpecializationId == specializationId);
        // Date-range filters (From = on-or-after, To = on-or-before; either
        // side optional). Compared on the date only.
        if (commencementFrom is { } cf)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.CommencementDate != null && e.CommencementDate >= cf.Date);
        if (commencementTo is { } cto)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.CommencementDate != null && e.CommencementDate <= cto.Date);
        if (graduationFrom is { } gf)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.GraduationDate != null && e.GraduationDate >= gf.Date);
        if (graduationTo is { } gt)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.GraduationDate != null && e.GraduationDate <= gt.Date);

        // Status-based access: hide enrolments whose status this role cannot see
        // (Hidden). SuperAdmin sees all; missing status rows default to Edit, so
        // this is a no-op until an admin sets a status to Hidden for the role.
        if (!perms.IsSuperAdmin(httpContext.User))
        {
            var statusMap = await perms.GetStatusMapAsync(httpContext.User, cancellationToken);
            var hidden = statusMap.Where(kv => kv.Value == (int)Odin.Api.Base.Authorization.AccessLevel.Hidden)
                .Select(kv => kv.Key).ToHashSet();
            if (hidden.Count > 0)
                enrollmentsQuery = enrollmentsQuery.Where(e => !hidden.Contains(e.StatusId));
        }

        var enrollments = await enrollmentsQuery
            .Select(e => new
            {
                e.StudentEnrollmentId,
                e.StudentId,
                e.PartnerId,
                e.SpecializationId,
                e.Specialization.ProgrammeId,
                ProgrammeCode = e.Specialization.Programmes.Code,
                ProgrammeName = e.Specialization.Programmes.Name,
                SpecializationName = e.Specialization.Name,
                e.ModeOfStudy.Name,
                e.CommencementDate,
                e.GraduationDate,
                StatusCode = e.Status.Code,
                StatusName = e.Status.Name,
                StatusLevel = e.Status.Level,
            })
            .ToListAsync(cancellationToken);

        // Enrolments with an unpaid installment or additional invoice past its
        // due date — powers the "Payment overdue" chip on the Students list.
        var enrollmentIds = enrollments.Select(e => e.StudentEnrollmentId).ToList();
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
        var overdueIds = enrollmentIds.Count == 0
            ? new HashSet<Guid>()
            : (await db.EnrollmentPaymentPlans
                .Where(p => enrollmentIds.Contains(p.StudentEnrollmentId) && p.DeletedAt == null
                    && (p.Installments.Any(i => !i.IsPaid && i.DueDate != null && i.DueDate < today)
                        || p.AdditionalInvoices.Any(a => !a.IsPaid && a.DueDate != null && a.DueDate < today)))
                .Select(p => p.StudentEnrollmentId)
                .ToListAsync(cancellationToken)).ToHashSet();

        // Paid-status column: any unpaid instalment/invoice at all (regardless
        // of due date), and whether a plan with items exists in the first place.
        var unpaidIds = enrollmentIds.Count == 0
            ? new HashSet<Guid>()
            : (await db.EnrollmentPaymentPlans
                .Where(p => enrollmentIds.Contains(p.StudentEnrollmentId) && p.DeletedAt == null
                    && (p.Installments.Any(i => !i.IsPaid) || p.AdditionalInvoices.Any(a => !a.IsPaid)))
                .Select(p => p.StudentEnrollmentId)
                .ToListAsync(cancellationToken)).ToHashSet();
        var planIds = enrollmentIds.Count == 0
            ? new HashSet<Guid>()
            : (await db.EnrollmentPaymentPlans
                .Where(p => enrollmentIds.Contains(p.StudentEnrollmentId) && p.DeletedAt == null
                    && (p.Installments.Any() || p.AdditionalInvoices.Any()))
                .Select(p => p.StudentEnrollmentId)
                .ToListAsync(cancellationToken)).ToHashSet();
        var paidIds = enrollmentIds.Count == 0
            ? new HashSet<Guid>()
            : (await db.EnrollmentPaymentPlans
                .Where(p => enrollmentIds.Contains(p.StudentEnrollmentId) && p.DeletedAt == null
                    && (p.Installments.Any(i => i.IsPaid) || p.AdditionalInvoices.Any(a => a.IsPaid)))
                .Select(p => p.StudentEnrollmentId)
                .ToListAsync(cancellationToken)).ToHashSet();

        // Group by student. Students with NO enrolment yet (mid-signup,
        // wizard steps 1–3) must appear too — the "Signing up" chip lives off
        // them — so the student set is enrolment students PLUS unfinished
        // signups, unless a programme/specialization filter excludes them.
        var studentIds = enrollments.Select(e => e.StudentId).Distinct().ToList();
        var studentsQuery = db.Students.Where(s => s.DeletedAt == null);
        // Sales logins see ONLY their own students (added by them or assigned
        // to them via Handled by) — across the whole portal.
        if (httpContext.User.IsInRole("Sales"))
        {
            var salesUserId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var assignedPartnerIds = await db.SalesPartnerAssignments
                .Where(a => a.UserId == salesUserId)
                .Select(a => a.PartnerId)
                .ToListAsync(cancellationToken);
            studentsQuery = studentsQuery.Where(s =>
                s.CreatedByUserId == salesUserId
                || s.HandledByUserId == salesUserId
                || assignedPartnerIds.Contains(s.PartnerId));
        }
        var dateFilterActive = commencementFrom is not null || commencementTo is not null
            || graduationFrom is not null || graduationTo is not null;
        if (programmeId is not null || specializationId is not null || dateFilterActive)
            // Date/programme/spec filters are enrolment-level: keep only
            // students who have a MATCHING enrolment.
            studentsQuery = studentsQuery.Where(s => studentIds.Contains(s.StudentId));
        else if (partnerId is not null)
            studentsQuery = studentsQuery.Where(s => studentIds.Contains(s.StudentId) || s.PartnerId == partnerId);
        var students = await studentsQuery
            .Select(s => new
            {
                s.StudentId,
                s.StudentNumber,
                s.WizardStep,
                s.UserId,
                s.PartnerId,
                s.CreatedByUserId,
                s.HandledByUserId,
                User = new { s.User.UserName, s.User.Email, s.User.EmailConfirmed },
                Profile = db.UserProfiles.Where(p => p.UserId == s.UserId)
                    .Select(p => new { p.FirstName, p.LastName }).FirstOrDefault(),
                PartnerName = db.Partners.Where(p => p.PartnerId == s.PartnerId).Select(p => p.Name).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var enrollmentsByStudent = enrollments.GroupBy(e => e.StudentId).ToDictionary(g => g.Key, g => g.ToList());

        // "Added by" labels: staff name + which office; null = self signup.
        var creatorIds = students.Where(s => s.CreatedByUserId != null).Select(s => s.CreatedByUserId!)
            .Concat(students.Where(s => s.HandledByUserId != null).Select(s => s.HandledByUserId!))
            .Distinct().ToList();
        var creators = creatorIds.Count == 0
            ? new Dictionary<string, string>()
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
                .ToListAsync(cancellationToken))
                .ToDictionary(
                    u => u.Id,
                    u => (string.IsNullOrWhiteSpace(u.Name) ? u.UserName : u.Name)
                        + (u.PartnerId != null ? " (partner)" : " (admission)"));

        var items = students.Select(s =>
        {
            var enrs = enrollmentsByStudent.GetValueOrDefault(s.StudentId) ?? new();
            // Still signing up = wizard never finished AND nothing beyond a
            // draft enrolment exists (guards legacy rows with WizardStep 0).
            var signingUp = s.WizardStep < 6 && enrs.All(e => e.StatusCode == "Draft");
            return new
            {
                studentId = s.StudentId,
                studentNumber = s.StudentNumber,
                allStudentIds = db.StudentIdentifiers.Where(i => i.StudentId == s.StudentId)
                    .Select(i => i.Value).ToList(),
                createdBy = s.CreatedByUserId != null ? creators.GetValueOrDefault(s.CreatedByUserId) : null,
                handledBy = s.HandledByUserId != null ? creators.GetValueOrDefault(s.HandledByUserId) : null,
                wizardStep = s.WizardStep,
                signingUp,
                username = s.User.UserName,
                email = s.User.Email,
                emailVerified = s.User.EmailConfirmed,
                firstName = s.Profile?.FirstName,
                lastName = s.Profile?.LastName,
                partnerId = s.PartnerId,
                partnerName = s.PartnerName,
                enrollments = enrs.Select(e => new
                {
                    studentEnrollmentId = e.StudentEnrollmentId,
                    programmeId = e.ProgrammeId,
                    programmeCode = e.ProgrammeCode,
                    programmeName = e.ProgrammeName,
                    specializationId = e.SpecializationId,
                    specializationName = e.SpecializationName,
                    modeOfStudyName = e.Name,
                    commencementDate = e.CommencementDate,
                    graduationDate = e.GraduationDate,
                    statusCode = e.StatusCode,
                    statusName = e.StatusName,
                    paymentOverdue = overdueIds.Contains(e.StudentEnrollmentId),
                    hasUnpaid = unpaidIds.Contains(e.StudentEnrollmentId),
                    hasPaymentPlan = planIds.Contains(e.StudentEnrollmentId),
                    hasPaid = paidIds.Contains(e.StudentEnrollmentId),
                }).ToList(),
            };
        }).ToList();

        // Per-statusCode counts span the unfiltered set so chip totals don't
        // collapse when the user clicks a chip. Frontend keys off statusCode.
        var countsByCode = items
            .SelectMany(i => i.enrollments.Select(e => e.statusCode))
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        var noEnrolment = items.Count(i => i.enrollments.Count == 0);

        return Results.Ok(new
        {
            items,
            total = items.Count,
            noEnrolment,
            countsByCode,
        });
    }
}
