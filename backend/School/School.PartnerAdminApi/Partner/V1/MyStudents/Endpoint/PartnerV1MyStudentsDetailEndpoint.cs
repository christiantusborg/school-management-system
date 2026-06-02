using School.PartnerAdminApi.Partner.V1.MyUsers;

namespace School.PartnerAdminApi.Partner.V1.MyStudents.Endpoint;

/// <summary>
/// Read-only student detail for the partner portal drawer.
/// Returns 404 if the student belongs to a different partner.
/// </summary>
[Route("/v1/partner/my-students/{studentId:guid}")]
[EndpointTag("Partner.MyStudents")]
public sealed class PartnerV1MyStudentsDetailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/partner/my-students/{studentId:guid}", HandleAsync).RequireAuthorization("PartnerOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var (_, partnerId, fail) = await MyUsersHelpers.ResolveAsync(httpContext, db, ct);
        if (fail is not null) return fail;

        var student = await db.Students
            .Where(s => s.StudentId == studentId && s.PartnerId == partnerId && s.DeletedAt == null)
            .Select(s => new
            {
                s.StudentId,
                s.StudentNumber,
                s.UserId,
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
            })
            .FirstOrDefaultAsync(ct);

        if (student is null) return Results.NotFound();

        var adminRoleId = await db.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        // Per-application docs: one set per enrolment, no cross-pollination.
        // The reviewer's wizard scopes to a single enrolment, so the partner
        // sees only the docs belonging to whatever enrolment they opened.
        var documents = await db.StudentDocuments
            .Where(d => d.StudentId == studentId
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
                status = d.CurrentStatus.Code,
                statusName = d.CurrentStatus.Name,
                isVerified = d.CurrentStatus.Code == "VerifiedByPartner"
                    || d.CurrentStatus.Code == "VerifiedByEnrolment",
                requirements = db.DocumentTypeVerifyRequirements
                    .Where(r => r.DocumentTypeId == d.DocumentTypeId && r.DeletedAt == null)
                    .OrderBy(r => r.Name)
                    .Select(r => new
                    {
                        id = r.DocumentTypeVerifyRequirementId,
                        name = r.Name,
                        rejectionLabel = r.RejectionLabel,
                    })
                    .ToList(),
                lastNote = db.StudentDocumentNotes
                    .Where(n => n.StudentDocumentId == d.StudentDocumentId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new
                    {
                        n.CreatedAt,
                        n.Note,
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

        // Same core-vs-additional partitioning as the admin endpoint: the
        // earliest non-deleted row per (enrollment, documentType) is the
        // core doc; later rows are additional uploads (post-approval
        // supplementary docs).
        var coreDocIds = documents
            .GroupBy(d => new { d.enrollmentId, d.documentTypeId })
            .Select(g => g.OrderBy(x => x.uploadedAt).First().studentDocumentId)
            .ToHashSet();

        var documentsOut = documents.Select(d => new
        {
            d.studentDocumentId,
            d.enrollmentId,
            d.documentTypeId,
            d.documentTypeName,
            d.fileName,
            d.uploadedAt,
            d.status,
            d.statusName,
            d.isVerified,
            isAdditional = !coreDocIds.Contains(d.studentDocumentId),
            d.requirements,
            lastChangedAt = d.lastNote?.CreatedAt,
            lastChangeReason = d.lastNote?.Note,
            lastChangedByName = d.lastNote == null
                ? null
                : d.lastNote.ActorIsAdmin
                    ? "Admission Office"
                    : string.Join(" ", new[] { d.lastNote.ActorFirstName, d.lastNote.ActorLastName }
                        .Where(p => !string.IsNullOrWhiteSpace(p))),
        }).ToList();

        // Canonical requirement list per wizard slot. The frontend uses these
        // as a fallback when the student hasn't uploaded a document for the
        // slot yet — otherwise the reviewer sees "No verification
        // requirements configured" on every empty step.
        var slotCanonicalNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["passport"] = "Passport",
            ["degree"]   = "Bachelor's Degree Certificate",
            ["language"] = "Language Proficiency Certificate",
            ["cv"]       = "Curriculum Vitae",
        };
        var canonicalNamesList = slotCanonicalNames.Values.ToList();
        var canonicalRequirements = await db.DocumentTypes
            .Where(t => t.DeletedAt == null && canonicalNamesList.Contains(t.Name))
            .Select(t => new
            {
                t.Name,
                Items = db.DocumentTypeVerifyRequirements
                    .Where(r => r.DocumentTypeId == t.DocumentTypeId && r.DeletedAt == null)
                    .OrderBy(r => r.Name)
                    .Select(r => new { id = r.DocumentTypeVerifyRequirementId, name = r.Name, rejectionLabel = r.RejectionLabel })
                    .ToList(),
            })
            .ToListAsync(ct);
        var canonicalByName = canonicalRequirements.ToDictionary(x => x.Name, x => x.Items, StringComparer.OrdinalIgnoreCase);
        var slotRequirementsOut = slotCanonicalNames.ToDictionary(
            kvp => kvp.Key,
            kvp => canonicalByName.TryGetValue(kvp.Value, out var items) ? items : new());

        var enrollmentsRaw = await db.Enrollments
            .Where(e => e.StudentId == studentId && e.DeletedAt == null && e.PartnerId == partnerId)
            .Select(e => new
            {
                studentEnrollmentId = e.StudentEnrollmentId,
                programmeId = e.Specialization.ProgrammeId,
                programmeCode = e.Specialization.Programmes.Code,
                programmeName = e.Specialization.Programmes.Name,
                programmeMinDurationMonths = e.Specialization.Programmes.MinDurationMonths,
                programmeMaxDurationMonths = e.Specialization.Programmes.MaxDurationMonths,
                specializationId = e.SpecializationId,
                specializationName = e.Specialization.Name,
                pathwayId = (int?)e.PathwayId,
                modeOfStudyId = e.ModeOfStudyId,
                modeOfStudyName = e.ModeOfStudy.Name,
                commencementDate = e.CommencementDate,
                durationOfStudyMonths = (int?)e.Specialization.DurationOfStudyMonths,
                approvedDurationMonths = e.ApprovedDurationMonths,
                tuitionFeeUsd = e.Specialization.TuitionFeeUsd,
                statusCode = e.Status.Code,
                statusName = e.Status.Name,
                statusLevel = e.Status.Level,
            })
            .ToListAsync(ct);

        // Released letter PDFs surfaced per enrolment so the partner detail
        // modal can render Download buttons. Same shape as the admin endpoint.
        object? PickLetter(Guid enrollmentId, Guid documentTypeId)
        {
            var d = documents
                .Where(x => x.enrollmentId == enrollmentId && x.documentTypeId == documentTypeId)
                .OrderByDescending(x => x.uploadedAt)
                .FirstOrDefault();
            return d is null ? null : new
            {
                studentDocumentId = d.studentDocumentId,
                fileName = d.fileName,
                uploadedAt = d.uploadedAt,
            };
        }

        var enrollments = enrollmentsRaw.Select(e => new
        {
            e.studentEnrollmentId,
            e.programmeId,
            e.programmeCode,
            e.programmeName,
            e.programmeMinDurationMonths,
            e.programmeMaxDurationMonths,
            e.specializationId,
            e.specializationName,
            e.pathwayId,
            e.modeOfStudyId,
            e.modeOfStudyName,
            e.commencementDate,
            e.durationOfStudyMonths,
            e.approvedDurationMonths,
            e.tuitionFeeUsd,
            e.statusCode,
            e.statusName,
            e.statusLevel,
            letters = new
            {
                offerLetter            = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.OfferLetter),
                admissionLetter        = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.AdmissionLetter),
                transcript             = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.Transcript),
                certificate            = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.Certificate),
                provisionalCertificate = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.ProvisionalCertificate),
            },
        }).ToList();

        var languages = await db.UserLanguages
            .Where(ul => ul.UserId == student.StudentId && ul.DeletedAt == null)
            .Select(ul => new
            {
                languageId = ul.LanguageId,
                proficiency = (int)ul.Proficiency,
            })
            .ToListAsync(ct);

        return Results.Ok(new
        {
            studentId = student.StudentId,
            studentNumber = student.StudentNumber,
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
                    line2 = (string?)null,
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
                languages,
            },
            documents = documentsOut,
            slotRequirements = slotRequirementsOut,
            enrollments,
        });
    }
}
