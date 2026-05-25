using SharedLibrary.Basics.Opaque.Domains;

namespace SharedLibrary.Basics.Opaque.StudentApi.V1.MeApplication.Endpoint;

/// <summary>
/// Returns the logged-in student's own application: account, personal,
/// per-enrolment status (with rejection summary if any), and per-document
/// status (with the latest <see cref="StudentDocumentNote"/> reason for
/// rejected docs). Mirrors the partner-side detail projection but scoped
/// to the caller and reorganised so each enrolment carries its own
/// document slot list.
/// </summary>
[Route("/v1/student/me/application")]
[EndpointTag("Student.MeApplication")]
public sealed class StudentV1MeApplicationEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/student/me/application", HandleAsync).RequireAuthorization("StudentOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var callerId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(callerId)) return Results.Unauthorized();

        var student = await db.Students
            .Where(s => s.UserId == callerId && s.DeletedAt == null)
            .Select(s => new
            {
                s.StudentId,
                s.StudentNumber,
                s.UserId,
                s.PartnerId,
                s.PassportId,
                s.DateOfBirth,
                s.HighestDegree,
                s.YearsWorkExperience,
                s.NationalityId,
                User = new { s.User.UserName, s.User.Email, s.User.EmailConfirmed },
                Profile = db.UserProfiles.Where(p => p.UserId == s.UserId)
                    .Select(p => new { p.FirstName, p.LastName }).FirstOrDefault(),
                Address = db.UserAddresses.Where(a => a.UserId == s.UserId && a.IsPrimary)
                    .Select(a => new { a.Street, a.City, a.State, a.ZipCode, a.Country }).FirstOrDefault(),
                Partner = db.Partners.Where(p => p.PartnerId == s.PartnerId)
                    .Select(p => new
                    {
                        p.Name,
                        ContactEmail = p.Emails.Where(e => e.DeletedAt == null)
                            .OrderByDescending(e => e.IsPrimary)
                            .Select(e => e.Email)
                            .FirstOrDefault(),
                    }).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        if (student is null) return Results.NotFound();

        var adminRoleId = await db.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        // Pull all the student's docs in one query; we'll partition by
        // EnrollmentId in memory to build the per-enrolment slot lists.
        // Each row is per-application after the schema change — no
        // cross-pollination between BBA and MBA.
        var documents = await db.StudentDocuments
            .Where(d => d.StudentId == student.StudentId
                && d.EnrollmentId != null
                && d.DeletedAt == null)
            .Select(d => new
            {
                studentDocumentId = d.StudentDocumentId,
                enrollmentId = d.EnrollmentId!.Value,
                documentTypeId = d.DocumentTypeId,
                documentTypeName = d.DocumentType.Name,
                fileName = d.FileName,
                uploadedAt = d.UploadedAt,
                statusCode = d.CurrentStatus.Code,
                statusName = d.CurrentStatus.Name,
                lastNote = db.StudentDocumentNotes
                    .Where(n => n.StudentDocumentId == d.StudentDocumentId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new
                    {
                        n.CreatedAt,
                        n.Note,
                        n.StatusId,
                        n.ByUserId,
                        ActorIsAdmin = adminRoleId != null
                            && db.UserRoles.Any(ur => ur.UserId == n.ByUserId && ur.RoleId == adminRoleId),
                        ActorFirstName = db.UserProfiles
                            .Where(p => p.UserId == n.ByUserId)
                            .Select(p => p.FirstName).FirstOrDefault(),
                        ActorLastName = db.UserProfiles
                            .Where(p => p.UserId == n.ByUserId)
                            .Select(p => p.LastName).FirstOrDefault(),
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);
        // Multi-row handling: per (enrolment, documentType) the OLDEST
        // non-deleted row is the canonical "core" doc — that's the one
        // shown in the required-document slot. Any later rows are
        // additional uploads (post-approval supplementary docs) and are
        // surfaced separately in additionalDocuments below.
        //
        // Letter document types (offer/admission/transcript/etc.) can
        // legitimately have multiple rows when re-released; the slot list
        // never references them so the OLDEST-wins rule doesn't matter
        // for letters. PickLetter takes the LATEST row explicitly.
        var docsByEnrolmentAll = documents
            .GroupBy(d => d.enrollmentId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var docsByEnrolment = docsByEnrolmentAll.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                .GroupBy(x => x.documentTypeId)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.uploadedAt).First()));

        var enrollments = await db.Enrollments
            .Where(e => e.StudentId == student.StudentId && e.DeletedAt == null)
            .Select(e => new
            {
                enrollmentId = e.StudentEnrollmentId,
                programmeId = e.Specialization.ProgrammeId,
                programmeCode = e.Specialization.Programmes.Code,
                programmeName = e.Specialization.Programmes.Name,
                specializationName = e.Specialization.Name,
                commencementDate = e.CommencementDate,
                durationOfStudyMonths = (int?)e.Specialization.DurationOfStudyMonths,
                tuitionFeeUsd = e.Specialization.TuitionFeeUsd,
                statusCode = e.Status.Code,
                statusName = e.Status.Name,
                statusLevel = e.Status.Level,
                lastStatusNote = db.Set<EnrollmentStatusNote>()
                    .Where(n => n.EnrollmentId == e.StudentEnrollmentId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new
                    {
                        n.CreatedAt,
                        n.Note,
                        n.StatusId,
                        n.ByUserId,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        // ProgrammeDocumentRequirement → which slots each programme demands.
        var programmeIds = enrollments.Select(e => e.programmeId).Distinct().ToList();
        var requirementsByProgramme = await db.ProgrammeDocumentRequirements
            .Where(r => programmeIds.Contains(r.ProgrammeId) && r.DeletedAt == null)
            .Select(r => new
            {
                r.ProgrammeId,
                r.DocumentTypeId,
                Name = r.DocumentType.Name,
            })
            .ToListAsync(ct);
        var requirementsLookup = requirementsByProgramme
            .GroupBy(r => r.ProgrammeId)
            .ToDictionary(g => g.Key, g => g.Select(r => new { r.DocumentTypeId, r.Name }).ToList());

        // Build the per-enrolment payload. The doc list is the same across
        // enrolments today (canonical 4 slots), but it lives under each
        // enrolment so the frontend can render multiple cards if a student
        // has more than one application.
        var enrollmentsOut = new List<(int rank, int level, object payload)>();
        foreach (var e in enrollments)
        {
            var isRejected = e.statusCode == "ApplicationRejectedByPartner"
                || e.statusCode == "ApplicationRejectedByAdmission";
            var canResubmit = isRejected;
            var canAcceptOffer = e.statusCode == "AcceptOffer";

            object? rejectionSummary = null;
            if (isRejected && e.lastStatusNote is not null)
            {
                var byName = await ResolveActorNameAsync(db, adminRoleId, e.lastStatusNote.ByUserId.ToString(), ct);
                rejectionSummary = new
                {
                    note = e.lastStatusNote.Note,
                    byName,
                    atUtc = e.lastStatusNote.CreatedAt,
                };
            }

            var requirements = requirementsLookup.TryGetValue(e.programmeId, out var reqs)
                ? reqs
                : new();
            var docByType = docsByEnrolment.TryGetValue(e.enrollmentId, out var byType)
                ? byType
                : new();

            // Released letter PDFs surfaced as their own block. The frontend
            // download buttons key off these ids — null means "not yet
            // released" (button stays disabled). Picks the most recent doc
            // per type in case the letter was re-released. Source list is
            // docsByEnrolmentAll (every row), not docByType (core only).
            object? PickLetter(Guid documentTypeId)
            {
                if (!docsByEnrolmentAll.TryGetValue(e.enrollmentId, out var list)) return null;
                var d = list
                    .Where(x => x.documentTypeId == documentTypeId)
                    .OrderByDescending(x => x.uploadedAt)
                    .FirstOrDefault();
                if (d is null) return null;
                return new
                {
                    studentDocumentId = d.studentDocumentId,
                    fileName = d.fileName,
                    uploadedAt = d.uploadedAt,
                };
            }
            var letters = new
            {
                offerLetter            = PickLetter(SystemDocumentTypeIds.OfferLetter),
                admissionLetter        = PickLetter(SystemDocumentTypeIds.AdmissionLetter),
                transcript             = PickLetter(SystemDocumentTypeIds.Transcript),
                certificate            = PickLetter(SystemDocumentTypeIds.Certificate),
                provisionalCertificate = PickLetter(SystemDocumentTypeIds.ProvisionalCertificate),
            };

            var requiredDocuments = new List<object>();
            foreach (var req in requirements)
            {
                docByType.TryGetValue(req.DocumentTypeId, out var d);

                var docIsRejected = d != null
                    && (d.statusCode == "RejectedByPartner" || d.statusCode == "RejectedByEnrolment");
                object? rejectionReasons = null;
                if (docIsRejected && d!.lastNote is not null)
                {
                    var byName = d.lastNote.ActorIsAdmin
                        ? "Admission Office"
                        : string.Join(" ", new[] { d.lastNote.ActorFirstName, d.lastNote.ActorLastName }
                            .Where(p => !string.IsNullOrWhiteSpace(p)));
                    rejectionReasons = new
                    {
                        note = d.lastNote.Note,
                        byName = string.IsNullOrWhiteSpace(byName) ? null : byName,
                        atUtc = d.lastNote.CreatedAt,
                    };
                }

                requiredDocuments.Add(new
                {
                    documentTypeId = req.DocumentTypeId,
                    name = req.Name,
                    uploaded = d != null,
                    studentDocumentId = d?.studentDocumentId,
                    fileName = d?.fileName,
                    uploadedAt = d?.uploadedAt,
                    statusCode = d?.statusCode,
                    statusName = d?.statusName,
                    isRejected = docIsRejected,
                    rejectionReasons,
                });
            }

            // Anything for this enrolment that is NOT the core doc of a
            // required slot AND not a system-generated letter is treated as
            // an additional supporting document.
            var coreDocIds = docByType.Values.Select(v => v.studentDocumentId).ToHashSet();
            var letterTypeIds = new HashSet<Guid>
            {
                SystemDocumentTypeIds.OfferLetter,
                SystemDocumentTypeIds.AdmissionLetter,
                SystemDocumentTypeIds.Transcript,
                SystemDocumentTypeIds.Certificate,
                SystemDocumentTypeIds.ProvisionalCertificate,
            };
            var additionalDocuments = (docsByEnrolmentAll.TryGetValue(e.enrollmentId, out var allDocsForEnr)
                    ? allDocsForEnr
                    : new())
                .Where(x => !coreDocIds.Contains(x.studentDocumentId)
                            && !letterTypeIds.Contains(x.documentTypeId))
                .OrderBy(x => x.uploadedAt)
                .Select(x => new
                {
                    studentDocumentId = x.studentDocumentId,
                    documentTypeId = x.documentTypeId,
                    documentTypeName = x.documentTypeName,
                    fileName = x.fileName,
                    uploadedAt = x.uploadedAt,
                    statusCode = x.statusCode,
                    statusName = x.statusName,
                })
                .ToList<object>();

            // Most-actionable first: rejected, then offer-ready, then accept-admission, then everything else.
            var rank = isRejected ? 0
                : canAcceptOffer ? 1
                : e.statusCode == "AcceptAdmission" ? 2
                : 3;

            enrollmentsOut.Add((rank, e.statusLevel, new
            {
                e.enrollmentId,
                e.programmeCode,
                e.programmeName,
                e.specializationName,
                e.commencementDate,
                e.durationOfStudyMonths,
                e.tuitionFeeUsd,
                e.statusCode,
                e.statusName,
                e.statusLevel,
                isRejected,
                rejectionSummary,
                requiredDocuments,
                additionalDocuments,
                letters,
                canResubmit,
                canAcceptOffer,
            }));
        }

        var ordered = enrollmentsOut
            .OrderBy(t => t.rank)
            .ThenByDescending(t => t.level)
            .Select(t => t.payload)
            .ToList();

        return Results.Ok(new
        {
            studentId = student.StudentId,
            studentNumber = student.StudentNumber,
            partner = student.Partner == null ? null : new
            {
                name = student.Partner.Name,
                contactEmail = student.Partner.ContactEmail,
            },
            account = new
            {
                username = student.User.UserName,
                email = student.User.Email,
                emailVerified = student.User.EmailConfirmed,
                firstName = student.Profile?.FirstName,
                lastName = student.Profile?.LastName,
            },
            personal = new
            {
                dateOfBirth = student.DateOfBirth,
                passportId = student.PassportId,
                nationalityId = student.NationalityId,
                address = new
                {
                    line1 = student.Address?.Street,
                    city = student.Address?.City,
                    stateRegion = student.Address?.State,
                    postalCode = student.Address?.ZipCode,
                    countryCode = student.Address?.Country,
                },
            },
            background = new
            {
                highestDegree = student.HighestDegree,
                yearsWorkExperience = student.YearsWorkExperience,
            },
            enrollments = ordered,
        });
    }

    private static async Task<string?> ResolveActorNameAsync(OdinDbContext db, string? adminRoleId, string actorUserId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(actorUserId) || actorUserId == Guid.Empty.ToString()) return null;
        var isAdmin = adminRoleId != null
            && await db.UserRoles.AnyAsync(ur => ur.UserId == actorUserId && ur.RoleId == adminRoleId, ct);
        if (isAdmin) return "Admission Office";
        var profile = await db.UserProfiles
            .Where(p => p.UserId == actorUserId)
            .Select(p => new { p.FirstName, p.LastName })
            .FirstOrDefaultAsync(ct);
        if (profile is null) return null;
        var name = string.Join(" ", new[] { profile.FirstName, profile.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
