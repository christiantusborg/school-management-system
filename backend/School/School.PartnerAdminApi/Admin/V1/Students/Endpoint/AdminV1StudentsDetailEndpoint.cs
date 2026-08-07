namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Read-only student detail for the admin (Admission Office) review wizard.
/// Mirrors <c>PartnerV1MyStudentsDetailEndpoint</c> with the partner-owner
/// filter removed — admins can read any student.
/// </summary>
[Route("/v1/admin/students/{studentId:guid}")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsDetailEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/{studentId:guid}", HandleAsync).RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> HandleAsync(
        Guid studentId, OdinDbContext db, CancellationToken ct)
    {
        var student = await db.Students
            .Where(s => s.StudentId == studentId && s.DeletedAt == null)
            .Select(s => new
            {
                s.StudentId,
                s.StudentNumber,
                s.IsLegacyStudent,
                s.MoodleEnabled,
                s.MoodleUsername,
                s.MoodlePassword,
                s.UserId,
                s.PassportId,
                s.StudentCardId,
                s.HandledByUserId,
                s.DateOfBirth,
                s.HighestDegree,
                s.DegreeSpecialization,
                s.YearsWorkExperience,
                s.NationalityId,
                s.PartnerId,
                User = new { s.User.UserName, s.User.Email, s.User.EmailConfirmed },
                Profile = db.UserProfiles.Where(p => p.UserId == s.UserId)
                    .Select(p => new { p.FirstName, p.LastName }).FirstOrDefault(),
                Address = db.UserAddresses.Where(a => a.UserId == s.UserId && a.IsPrimary)
                    .Select(a => new { a.Street, a.City, a.State, a.ZipCode, a.Country }).FirstOrDefault(),
                PartnerName = db.Partners.Where(p => p.PartnerId == s.PartnerId).Select(p => p.Name).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(ct);

        if (student is null) return Results.NotFound();

        var adminRoleId = await db.Roles
            .Where(r => r.Name == "Admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

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
                aiConfidence = d.AiConfidence,
                aiFraudRisk = d.AiFraudRisk,
                aiScannable = !d.DocumentType.IsSystemGenerated,
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

        // Per (enrollmentId, documentTypeId) the OLDEST non-deleted row is
        // the canonical "core" doc; any later rows are additional uploads.
        // Today that mostly affects post-approval additional uploads — the
        // student's pre-approval replace flow soft-deletes priors, so only
        // one row per slot survives there. Letter-type docs are also
        // multi-row by design (re-released) but never appear in the slot
        // UI, so the "additional" label on those is harmless.
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
            d.aiConfidence,
            d.aiFraudRisk,
            d.aiScannable,
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

        // Keep bulk-agreement assignments fresh for this student's partners.
        foreach (var pid in await db.Enrollments
            .Where(e => e.StudentId == studentId && e.DeletedAt == null && e.BulkAgreementId == null)
            .Select(e => e.PartnerId).Distinct().ToListAsync(ct))
        {
            try { await School.PartnerAdminApi.Admin.V1.Partners.BulkAgreements.AdminV1BulkAgreementsEndpoint.EnsureAssignmentsAsync(db, pid, ct); }
            catch { /* assignment refresh is best-effort */ }
        }

        var enrollmentsRaw = await db.Enrollments
            .Where(e => e.StudentId == studentId && e.DeletedAt == null)
            .Select(e => new
            {
                studentEnrollmentId = e.StudentEnrollmentId,
                programmeId = e.Specialization.ProgrammeId,
                programmeCode = e.Specialization.Programmes.Code,
                programmeName = e.Specialization.Programmes.Name,
                enrolmentPartnerId = e.PartnerId,
                enrolmentPartnerName = db.Partners.Where(pa => pa.PartnerId == e.PartnerId).Select(pa => pa.Name).FirstOrDefault(),
                projectApprovalPassMark = e.Specialization.Programmes.ProjectApprovalPassMark,
                bulkAgreementId = e.BulkAgreementId,
                bulkAgreementNumber = e.BulkAgreementId != null
                    ? db.BulkAgreements.Where(b => b.BulkAgreementId == e.BulkAgreementId).Select(b => b.AgreementNumber).FirstOrDefault()
                    : null,
                programmeMinDurationMonths = e.Specialization.Programmes.MinDurationMonths,
                programmeMaxDurationMonths = e.Specialization.Programmes.MaxDurationMonths,
                specializationId = e.SpecializationId,
                specializationName = e.Specialization.Name,
                // Effective language (override wins) for display; raw override for editing.
                instructionLanguage = e.InstructionLanguageOverride != null && e.InstructionLanguageOverride != ""
                    ? e.InstructionLanguageOverride
                    : e.Specialization.InstructionLanguage,
                instructionLanguageOverride = e.InstructionLanguageOverride,
                offerLetterDate = e.OfferLetterDate,
                admissionLetterDate = e.AdmissionLetterDate,
                transcriptDate = e.TranscriptDate,
                graduationDate = e.GraduationDate,
                pathwayId = (int?)e.PathwayId,
                modeOfStudyId = e.ModeOfStudyId,
                modeOfStudyName = e.ModeOfStudy.Name,
                commencementDate = e.CommencementDate,
                durationOfStudyMonths = (int?)e.Specialization.DurationOfStudyMonths,
                approvedDurationValue = e.ApprovedDurationValue,
                approvedDurationUnit = e.ApprovedDurationUnit,
                tuitionFeeUsd = e.Specialization.TuitionFeeUsd,
                statusCode = e.Status.Code,
                statusName = e.Status.Name,
                statusLevel = e.Status.Level,
            })
            .ToListAsync(ct);

        // Released letter PDFs surfaced per enrolment so the admin detail
        // modal can render Download buttons. Picks the latest doc of each
        // letter type from the already-loaded `documents` list — no extra
        // queries. The frontend hits AdminV1StudentsDocumentFileEndpoint
        // with the studentDocumentId.
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

        // Config-created (dynamic) letter types: one row per active
        // definition per enrolment, with the released document when present.
        // Admission sees every type regardless of visibility switches.
        var letterDefinitions = await db.LetterTypeDefinitions
            .Where(d => d.DeletedAt == null)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .Select(d => new
            {
                d.LetterTypeDefinitionId,
                d.Name,
                d.DocumentTypeId,
                d.VisibleToStudent,
                d.VisibleToPartner,
                d.AllowLegacyUpload,
                d.EmailOnRelease,
            })
            .ToListAsync(ct);

        var enrollments = enrollmentsRaw.Select(e => new
        {
            e.studentEnrollmentId,
            e.programmeId,
            e.programmeCode,
            e.programmeName,
            e.enrolmentPartnerId,
            e.enrolmentPartnerName,
            e.projectApprovalPassMark,
            e.bulkAgreementId,
            e.bulkAgreementNumber,
            e.programmeMinDurationMonths,
            e.programmeMaxDurationMonths,
            e.specializationId,
            e.specializationName,
            e.instructionLanguage,
            e.instructionLanguageOverride,
            e.offerLetterDate,
            e.admissionLetterDate,
            e.transcriptDate,
            e.graduationDate,
            e.pathwayId,
            e.modeOfStudyId,
            e.modeOfStudyName,
            e.commencementDate,
            e.durationOfStudyMonths,
            e.approvedDurationValue,
            e.approvedDurationUnit,
            e.tuitionFeeUsd,
            e.statusCode,
            e.statusName,
            e.statusLevel,
            letters = new
            {
                offerLetter            = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.OfferLetter),
                admissionLetter        = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.AdmissionLetter),
                transcript             = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.Transcript),
                printableTranscript    = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.PrintableTranscript),
                certificate            = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.Certificate),
                provisionalCertificate = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.ProvisionalCertificate),
                studentIdCard          = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.StudentIdCard),
                finalProposalApproval  = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.FinalProposalApproval),
                finalProjectApproval   = PickLetter(e.studentEnrollmentId, SystemDocumentTypeIds.FinalProjectApproval),
            },
            dynamicLetters = letterDefinitions.Select(d => new
            {
                letterTypeDefinitionId = d.LetterTypeDefinitionId,
                name = d.Name,
                visibleToStudent = d.VisibleToStudent,
                visibleToPartner = d.VisibleToPartner,
                allowLegacyUpload = d.AllowLegacyUpload,
                emailOnRelease = d.EmailOnRelease,
                letter = PickLetter(e.studentEnrollmentId, d.DocumentTypeId),
            }).ToList(),
            issueDigitalStudentCard = db.Programmes
                .Where(p => p.ProgrammeId == e.programmeId)
                .Select(p => p.IssueDigitalStudentCard)
                .FirstOrDefault(),
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
            identifiers = await db.StudentIdentifiers
                .Where(i => i.StudentId == studentId)
                .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.CreatedAt)
                .Select(i => new { studentIdentifierId = i.StudentIdentifierId, value = i.Value, label = i.Label, isPrimary = i.IsPrimary })
                .ToListAsync(ct),
            handledByUserId = student.HandledByUserId,
            isLegacyStudent = student.IsLegacyStudent,
            moodleEnabled = student.MoodleEnabled,
            moodleUsername = student.MoodleUsername,
            moodlePassword = student.MoodlePassword,
            partner = new { partnerId = student.PartnerId, name = student.PartnerName },
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
                studentCardId = student.StudentCardId,
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
                degreeSpecialization = student.DegreeSpecialization,
                yearsWorkExperience = student.YearsWorkExperience,
                languages,
            },
            documents = documentsOut,
            slotRequirements = slotRequirementsOut,
            enrollments,
        });
    }
}
