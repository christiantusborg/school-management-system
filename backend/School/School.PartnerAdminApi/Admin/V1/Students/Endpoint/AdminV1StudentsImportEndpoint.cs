using System.Globalization;
using System.Security.Claims;
using System.Text;
using Odin.Api.Base.Authorization;
using PartnerEntity = SharedLibrary.Basics.Opaque.Domains.Partners.Partner;

namespace School.PartnerAdminApi.Admin.V1.Students.Endpoint;

/// <summary>
/// Bulk CSV student import for the Admission Office (admin Import tab).
/// One CSV line = one student x one programme enrolment; a student joining
/// several programmes appears on several lines, matched by StudentNumber
/// (or by Email within the same file when the number is blank).
///
/// Row rules:
///   - StudentNumber blank: a new student is created with a generated
///     ST-number (wizard format). Requires FirstName, LastName, Email.
///   - StudentNumber known: the row ONLY adds the enrolment; every personal
///     column is ignored so existing data is never overwritten.
///   - StudentNumber unknown: a new student is created keeping that
///     externally assigned number, flagged IsLegacyStudent.
///
/// New students get a login account (OPAQUE credentials, email-confirmed)
/// but NO email is sent. Enrolment status is per partner:
/// Partner.ImportDirectAdmission true (default) lands AwaitingGradesSubmit
/// (directly admitted, skipping the offer/letter pipeline); false lands
/// ApplicationAwaitingReviewByAdmission (the normal admission queue).
///
/// Routes: GET  .../import/sample          downloadable CSV template
///         POST .../import/validate        dry run, nothing written
///         POST .../import                 performs the import
///         POST .../import/partner-setting toggles ImportDirectAdmission
/// </summary>
[Route("/v1/admin/students/import")]
[EndpointTag("Admin.Students")]
public sealed class AdminV1StudentsImportEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/admin/students/import/sample", Sample)
            .RequireAuthorization("AdminOnly");
        app.MapGet("/v1/admin/students/import/help", HelpAsync)
            .RequireAuthorization("AdminOnly");
        app.MapPost("/v1/admin/students/import/validate", ValidateAsync)
            .RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapPost("/v1/admin/students/import", ImportAsync)
            .RequireAuthorization("AdminOnly").DisableAntiforgery();
        app.MapPost("/v1/admin/students/import/partner-setting", PartnerSettingAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    // ── CSV shape ──────────────────────────────────────────────────────────

    private static readonly string[] Columns =
    {
        "StudentNumber", "PartnerNumber", "FirstName", "LastName", "Email",
        "ProgrammeCode", "SpecializationCode", "ModeOfStudy",
        "CommencementDate", "DurationOfStudyMonths", "InstructionLanguage",
        "DateOfBirth", "PassportId", "NationalityCode", "Gender",
        "DisabilityDisclosure", "DisabilitySupportNeeds",
        "AddressLine1", "AddressLine2", "City", "StateRegion", "PostalCode",
        "CountryCode", "Phone",
        "HighestDegree", "DegreeSpecialization", "YearsWorkExperience",
        "PositionFunction", "EmploymentIndustry",
        "MonthlySalaryAmount", "MonthlySalaryCurrency",
        "WantsStudentCard",
    };

    private static async Task<IResult> Sample(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct,
        [FromQuery] Guid? partnerId = null)
    {
        var scoped = IsTruthy(httpContext.Request.Query["scoped"].ToString());
        // With a partner known, the sample is generated from THAT partner's
        // real programmes — one row per programme/specialization combination.
        if (partnerId is not null)
            return await PartnerSampleFileAsync(db, scoped, partnerId.Value, ct);
        return SampleFile(scoped);
    }

    /// <summary>
    /// Live sample for one partner: a fully filled example row for EVERY
    /// (programme, specialization) combination the partner can enrol into,
    /// using the real codes plus each specialization's default duration and
    /// instruction language — copy a row, replace the personal data, done.
    /// </summary>
    internal static async Task<IResult> PartnerSampleFileAsync(
        OdinDbContext db, bool scoped, Guid partnerId, CancellationToken ct)
    {
        var partnerInfo = await db.Partners
            .Where(p => p.PartnerId == partnerId)
            .Select(p => new { p.PartnerNumber, p.Slug })
            .FirstOrDefaultAsync(ct);
        var partnerNumber = partnerInfo?.PartnerNumber ?? "";
        // Partner-specific sample emails so a sample row imported by mistake
        // at one partner never collides with another partner's sample.
        var emailSlug = string.IsNullOrWhiteSpace(partnerInfo?.Slug) ? "sample" : partnerInfo!.Slug;
        var granted = await db.ProgrammePartners
            .Where(pp => pp.PartnerId == partnerId && pp.IsActive != null)
            .Select(pp => pp.ProgrammeId)
            .ToListAsync(ct);
        var programmes = await db.Programmes
            .Where(p => p.DeletedAt == null && (granted.Contains(p.ProgrammeId) || p.OwnerId == partnerId))
            .OrderBy(p => p.Code)
            .Select(p => new { p.ProgrammeId, p.Code })
            .ToListAsync(ct);
        var programmeIds = programmes.Select(p => p.ProgrammeId).ToList();
        var specs = await db.Specializations
            .Where(s => s.DeletedAt == null && programmeIds.Contains(s.ProgrammeId))
            .OrderBy(s => s.Code)
            .Select(s => new { s.ProgrammeId, s.Code, s.DurationOfStudyMonths, s.InstructionLanguage })
            .ToListAsync(ct);
        var firstModeId = await db.ModesOfStudy
            .OrderBy(m => m.ModeOfStudyId)
            .Select(m => (int?)m.ModeOfStudyId)
            .FirstOrDefaultAsync(ct) ?? 1;

        // Next 1 September as a plausible commencement.
        var now = DateTime.UtcNow;
        var commencement = new DateTime(now.Month >= 9 ? now.Year + 1 : now.Year, 9, 1);

        var columns = scoped ? Columns.Where(c => c != "PartnerNumber").ToArray() : Columns;
        string Row(params (string Col, string Val)[] vals)
        {
            var map = vals.ToDictionary(v => v.Col, v => v.Val, StringComparer.OrdinalIgnoreCase);
            return string.Join(',', columns.Select(c => CsvEscape(map.GetValueOrDefault(c, ""))));
        }

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', columns));
        var i = 0;
        foreach (var p in programmes)
        foreach (var s in specs.Where(s => s.ProgrammeId == p.ProgrammeId))
        {
            i++;
            sb.AppendLine(Row(
                ("PartnerNumber", partnerNumber),
                ("FirstName", "Sample"), ("LastName", $"Student{i}"),
                ("Email", $"{emailSlug}.student{i}@example.com"),
                ("ProgrammeCode", p.Code), ("SpecializationCode", s.Code),
                ("ModeOfStudy", firstModeId.ToString()),
                ("CommencementDate", commencement.ToString("yyyy-MM-dd")),
                ("DurationOfStudyMonths", s.DurationOfStudyMonths > 0 ? s.DurationOfStudyMonths.ToString() : ""),
                ("InstructionLanguage", string.IsNullOrWhiteSpace(s.InstructionLanguage) ? "English" : s.InstructionLanguage),
                ("DateOfBirth", "1992-04-17"), ("PassportId", $"P12345{i:D2}"), ("NationalityCode", "DK"),
                ("Gender", "Female"), ("DisabilityDisclosure", "No"),
                ("AddressLine1", "Main Street 1"), ("City", "Copenhagen"),
                ("PostalCode", "2100"), ("CountryCode", "DK"), ("Phone", "+45 12 34 56 78"),
                ("HighestDegree", "Bachelor"), ("DegreeSpecialization", "Marketing"),
                ("YearsWorkExperience", "4"),
                ("MonthlySalaryAmount", "3500"), ("MonthlySalaryCurrency", "USD"),
                ("WantsStudentCard", "true")));
        }
        if (i == 0)
            return SampleFile(scoped); // partner has no programmes yet — generic sample

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return Results.File(bytes, "text/csv", "student-import-sample.csv");
    }

    /// <summary>Downloadable .txt explaining every column + the live system
    /// values. ?partnerId= scopes the programme list to one partner.</summary>
    private static async Task<IResult> HelpAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct,
        [FromQuery] Guid? partnerId = null)
    {
        var text = await ImportHelpText.BuildAsync(db, scoped: partnerId is not null, partnerId, ct);
        return Results.File(Encoding.UTF8.GetBytes(text), "text/plain", "import-help.txt");
    }

    private static bool IsTruthy(string? v) => v is "1" or "true" or "True";

    internal static IResult SampleFile(bool scoped)
    {
        // Scoped samples (partner portal / admin partner view) have no
        // PartnerNumber column: the partner is implied by where you upload.
        var columns = scoped ? Columns.Where(c => c != "PartnerNumber").ToArray() : Columns;
        string Row(params (string Col, string Val)[] vals)
        {
            var map = vals.ToDictionary(v => v.Col, v => v.Val, StringComparer.OrdinalIgnoreCase);
            return string.Join(',', columns.Select(c => CsvEscape(map.GetValueOrDefault(c, ""))));
        }

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', columns));
        // New student (blank StudentNumber = auto-generated) joining one programme.
        sb.AppendLine(Row(
            ("PartnerNumber", "PA-20260101-AB12CD"),
            ("FirstName", "Jane"), ("LastName", "Doe"), ("Email", "jane.doe@example.com"),
            ("ProgrammeCode", "BBA"), ("SpecializationCode", "BBA-GEN"), ("ModeOfStudy", "1"),
            ("CommencementDate", "2026-09-01"), ("DurationOfStudyMonths", "24"),
            ("InstructionLanguage", "English"),
            ("DateOfBirth", "1992-04-17"), ("PassportId", "P1234567"), ("NationalityCode", "DK"),
            ("Gender", "Female"), ("DisabilityDisclosure", "No"),
            ("AddressLine1", "Main Street 1, 2nd floor"), ("City", "Copenhagen"),
            ("PostalCode", "2100"), ("CountryCode", "DK"), ("Phone", "+45 12 34 56 78"),
            ("HighestDegree", "Bachelor"), ("DegreeSpecialization", "Marketing"),
            ("YearsWorkExperience", "4"), ("PositionFunction", "Manager"),
            ("EmploymentIndustry", "Education"), ("MonthlySalaryAmount", "3500"),
            ("MonthlySalaryCurrency", "USD"), ("WantsStudentCard", "true")));
        // Same student joining a second programme: repeat the StudentNumber,
        // personal columns can stay empty (they are ignored for known numbers).
        sb.AppendLine(Row(
            ("StudentNumber", "ST-20260101-XY99ZZ"),
            ("PartnerNumber", "PA-20260101-AB12CD"),
            ("ProgrammeCode", "BBA"), ("SpecializationCode", "BBA-FIN"), ("ModeOfStudy", "1"),
            ("CommencementDate", "2026-09-01"), ("DurationOfStudyMonths", "18"),
            ("InstructionLanguage", "English")));

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return Results.File(bytes, "text/csv", "student-import-sample.csv");
    }

    // ── Endpoints ──────────────────────────────────────────────────────────

    private static async Task<IResult> ValidateAsync(
        HttpContext httpContext, OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager, CancellationToken ct)
    {
        var (forced, fail) = await ResolveForcedPartnerAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        return await ValidateCoreAsync(db, userManager, httpContext.Request, forced, ct);
    }

    internal static async Task<IResult> ValidateCoreAsync(
        OdinDbContext db, UserManager<ApplicationUser> userManager,
        HttpRequest request, PartnerEntity? forcedPartner, CancellationToken ct)
    {
        var (plans, partners, fileError) = await BuildPlansAsync(db, userManager, request, forcedPartner, ct);
        if (fileError is not null) return Results.BadRequest(new { error = fileError });

        var rows = plans.Select(p => new
        {
            row = p.RowNumber,
            studentNumber = p.ExistingStudent?.StudentNumber ?? p.GivenStudentNumber ?? "(auto)",
            student = p.DisplayName,
            partner = p.Partner?.Name,
            programme = p.ProgrammeName,
            specialization = p.SpecializationName,
            action = p.Errors.Count > 0 ? "error" : p.SkipReason is not null ? "skip"
                : p.ExistingStudent is not null || p.ReusesEarlierRow ? "enrol existing" : "create + enrol",
            message = p.Errors.Count > 0 ? string.Join(" ", p.Errors)
                : p.SkipReason ?? (p.AttachUser is not null
                    ? "Existing account found — a student will be attached to it." : ""),
        }).ToList();

        return Results.Ok(new
        {
            total = rows.Count,
            create = rows.Count(r => r.action == "create + enrol"),
            enroll = rows.Count(r => r.action == "enrol existing"),
            skip = rows.Count(r => r.action == "skip"),
            errors = rows.Count(r => r.action == "error"),
            partners,
            rows,
        });
    }

    private static async Task<IResult> ImportAsync(
        HttpContext httpContext, IPermissionService perms, OdinDbContext db,
        [FromServices] OpaqueUserCreationService creator,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] Odin.Api.Base.Email.StudentEmailVerificationSender verificationSender,
        CancellationToken ct)
    {
        if (await perms.AccessAsync(httpContext.User, "import.students", ct) != AccessLevel.Edit) return Results.Forbid();

        var (forced, fail) = await ResolveForcedPartnerAsync(httpContext, db, ct);
        if (fail is not null) return fail;
        var actorId = Guid.TryParse(
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var g) ? g : Guid.Empty;
        return await ImportCoreAsync(db, creator, userManager, verificationSender, httpContext.Request, forced, actorId, ct);
    }

    internal static async Task<IResult> ImportCoreAsync(
        OdinDbContext db, OpaqueUserCreationService creator,
        UserManager<ApplicationUser> userManager,
        Odin.Api.Base.Email.StudentEmailVerificationSender verificationSender,
        HttpRequest request, PartnerEntity? forcedPartner, Guid actorId, CancellationToken ct)
    {
        var (plans, partners, fileError) = await BuildPlansAsync(db, userManager, request, forcedPartner, ct);
        if (fileError is not null) return Results.BadRequest(new { error = fileError });

        var createdByKey = new Dictionary<string, Student>(StringComparer.OrdinalIgnoreCase);
        var usedNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<object>();
        int created = 0, enrolled = 0, skipped = 0, failed = 0;

        foreach (var plan in plans)
        {
            if (plan.Errors.Count > 0)
            {
                failed++;
                results.Add(RowResult(plan, "error", string.Join(" ", plan.Errors), null));
                continue;
            }
            if (plan.SkipReason is not null)
            {
                // The programme is skipped but a NEW Student ID on the row is
                // still attached to the matched student.
                var aliasNote = "";
                if (plan.ExistingStudent is not null && plan.AddAliasNumber is { } aliasNum
                    && usedNumbers.Add(aliasNum))
                {
                    db.StudentIdentifiers.Add(new SharedLibrary.Basics.Opaque.Domains.StudentIdentifier
                    {
                        StudentId = plan.ExistingStudent.StudentId,
                        Value = aliasNum,
                        Label = "CSV import",
                        IsPrimary = false,
                        CreatedAt = DateTime.UtcNow,
                    });
                    await db.SaveChangesAsync(ct);
                    aliasNote = $" New Student ID '{aliasNum}' attached.";
                }
                skipped++;
                results.Add(RowResult(plan, "skip", plan.SkipReason + aliasNote,
                    plan.ExistingStudent?.StudentNumber ?? plan.GivenStudentNumber));
                continue;
            }

            Student student;
            var createdNow = false;
            if (plan.ExistingStudent is not null)
            {
                student = plan.ExistingStudent;
            }
            else if (createdByKey.TryGetValue(plan.NewKey, out var earlier))
            {
                student = earlier;
            }
            else
            {
                // New student, OR attach a student to an existing plain account.
                ApplicationUser user;
                var attaching = plan.AttachUser is not null;
                if (attaching)
                {
                    // Reuse the existing login: just add the Student role and
                    // stamp the partner if it has none yet.
                    user = plan.AttachUser!;
                    if (!await userManager.IsInRoleAsync(user, "Student"))
                        await userManager.AddToRoleAsync(user, "Student");
                    if (user.PartnerId is null)
                    {
                        user.PartnerId = plan.Partner!.PartnerId;
                        await userManager.UpdateAsync(user);
                    }
                }
                else
                {
                    // New account: the service saves + throws on username collision.
                    try
                    {
                        (user, _) = await creator.CreateUserAsync(
                            plan.Email!, plan.Email!, "Student", plan.Partner!.PartnerId, ct);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        results.Add(RowResult(plan, "error", $"Account creation failed: {ex.Message}", null));
                        continue;
                    }
                }

                // ST-<commencement>-<monthly no> for the row's commencement; a
                // temporary number if the row has none (finalised when set).
                var number = plan.GivenStudentNumber
                    ?? (plan.CommencementDate is { } cd
                        ? await Odin.Api.Base.Students.StudentNumberService.MintAsync(db, cd, ct)
                        : Odin.Api.Base.Students.StudentNumberService.Temp());
                usedNumbers.Add(number);
                plan.WasAttached = attaching;
                student = new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user.Id,
                    StudentNumber = number,
                    IsLegacyStudent = plan.GivenStudentNumber is not null,
                    PartnerId = plan.Partner!.PartnerId,
                    WizardStep = 6,
                    CreatedByUserId = actorId == Guid.Empty ? null : actorId.ToString(),
                    PassportId = plan.Cell("PassportId"),
                    DateOfBirth = plan.DateOfBirth,
                    NationalityId = plan.NationalityId,
                    Gender = plan.Cell("Gender"),
                    DisabilityDisclosure = plan.Cell("DisabilityDisclosure"),
                    DisabilitySupportNeeds = plan.Cell("DisabilitySupportNeeds"),
                    HighestDegree = plan.Cell("HighestDegree"),
                    DegreeSpecialization = plan.Cell("DegreeSpecialization"),
                    YearsWorkExperience = plan.YearsWork,
                    CurrentPositionFunctionId = plan.PositionFunctionId,
                    CurrentEmploymentIndustryId = plan.EmploymentIndustryId,
                    MonthlySalaryAmount = plan.SalaryAmount,
                    MonthlySalaryCurrencyId = plan.CurrencyId,
                    WantsStudentIdCard = plan.WantsCard,
                };
                db.Students.Add(student);
                // When attaching to an existing account, only add profile/contact
                // rows it doesn't already have (they'd violate the 1:1/primary
                // constraints otherwise).
                if (!attaching || !await db.UserProfiles.AnyAsync(p => p.UserId == user.Id, ct))
                    db.UserProfiles.Add(new UserProfile
                    {
                        UserId = user.Id,
                        FirstName = plan.Cell("FirstName"),
                        LastName = plan.Cell("LastName"),
                        DateOfBirth = plan.DateOfBirth,
                    });
                if (!attaching || !await db.UserContactEmails.AnyAsync(c => c.UserId == user.Id, ct))
                    db.UserContactEmails.Add(new UserContactEmail
                    {
                        UserId = user.Id,
                        Email = plan.Email!,
                        IsPrimary = true,
                        IsVerified = true,
                    });
                var street = plan.Cell("AddressLine1");
                var line2 = plan.Cell("AddressLine2");
                if (street is not null && line2 is not null) street = $"{street}, {line2}";
                street ??= line2;
                if ((street is not null || plan.Cell("City") is not null
                    || plan.Cell("PostalCode") is not null || plan.Cell("CountryCode") is not null)
                    && (!attaching || !await db.UserAddresses.AnyAsync(a => a.UserId == user.Id && a.IsPrimary, ct)))
                {
                    db.UserAddresses.Add(new UserAddress
                    {
                        UserId = user.Id,
                        Street = street,
                        City = plan.Cell("City"),
                        State = plan.Cell("StateRegion"),
                        ZipCode = plan.Cell("PostalCode"),
                        Country = plan.Cell("CountryCode"),
                        IsPrimary = true,
                    });
                }
                if (plan.Cell("Phone") is { } phone
                    && (!attaching || !await db.UserPhones.AnyAsync(ph => ph.UserId == user.Id && ph.IsPrimary, ct)))
                    db.UserPhones.Add(new UserPhone { UserId = user.Id, Number = phone, IsPrimary = true });

                db.StudentIdentifiers.Add(new SharedLibrary.Basics.Opaque.Domains.StudentIdentifier
                {
                    StudentId = student.StudentId,
                    Value = number,
                    Label = plan.GivenStudentNumber is not null ? "CSV import" : null,
                    IsPrimary = true,
                    CreatedAt = DateTime.UtcNow,
                });

                createdByKey[plan.NewKey] = student;
                createdNow = true;
            }

            var direct = plan.Partner!.ImportDirectAdmission;
            var statusId = direct
                ? SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.AwaitingGradesSubmit
                : SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusIds.ApplicationAwaitingReviewByAdmission;
            var enrollmentId = Guid.NewGuid();
            db.Enrollments.Add(new SharedLibrary.Basics.Opaque.Domains.Enrollment
            {
                StudentEnrollmentId = enrollmentId,
                StudentId = student.StudentId,
                // Multi-partner: the enrolment belongs to the IMPORTING
                // partner, which may differ from the student's original one.
                PartnerId = plan.Partner!.PartnerId,
                SpecializationId = plan.SpecializationId,
                ModeOfStudyId = plan.ModeOfStudyId,
                PathwayId = 0,
                StatusId = statusId,
                CommencementDate = plan.CommencementDate,
                ApprovedDurationValue = plan.DurationMonths,
                ApprovedDurationUnit = plan.DurationMonths is null ? null : SharedLibrary.Basics.Opaque.Domains.DurationDays.UnitMonth,
                InstructionLanguageOverride = plan.InstructionLanguage,
            });
            db.EnrollmentStatusNotes.Add(new SharedLibrary.Basics.Opaque.Domains.EnrollmentStatusNote
            {
                EnrollmentStatusNoteId = Guid.NewGuid(),
                EnrollmentId = enrollmentId,
                StatusId = statusId,
                Note = direct
                    ? "Imported by Admission Office (direct admission)."
                    : "Imported by Admission Office (awaiting admission review).",
                ByUserId = actorId,
                CreatedAt = DateTime.UtcNow,
            });
            if (plan.ExistingStudent is not null && plan.AddAliasNumber is { } newAlias
                && usedNumbers.Add(newAlias))
            {
                db.StudentIdentifiers.Add(new SharedLibrary.Basics.Opaque.Domains.StudentIdentifier
                {
                    StudentId = student.StudentId,
                    Value = newAlias,
                    Label = "CSV import",
                    IsPrimary = false,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            await db.SaveChangesAsync(ct);

            // Attached students: send the welcome / login invite to the existing
            // account (best-effort — a mail hiccup never fails the import row).
            if (plan.WasAttached && plan.AttachUser?.Email is { } attachEmail)
            {
                try { await verificationSender.IssueAndSendAsync(plan.AttachUser.Id, attachEmail, ct); }
                catch { /* import row still succeeds */ }
            }

            if (createdNow) created++; else enrolled++;
            var enrolledMsg = plan.WasAttached
                ? $"Attached to existing account · enrolled in {plan.ProgrammeName} / {plan.SpecializationName}."
                : $"Enrolled in {plan.ProgrammeName} / {plan.SpecializationName}.";
            results.Add(RowResult(plan, createdNow ? "created" : "enrolled",
                enrolledMsg, student.StudentNumber));
        }

        return Results.Ok(new
        {
            total = plans.Count, created, enrolled, skipped, errors = failed,
            partners, rows = results,
        });
    }

    private sealed class PartnerSettingBody
    {
        public string? PartnerNumber { get; init; }
        public bool DirectAdmission { get; init; }
    }

    private static async Task<IResult> PartnerSettingAsync(
        [FromBody] PartnerSettingBody body, OdinDbContext db, CancellationToken ct)
    {
        var number = body.PartnerNumber?.Trim();
        if (string.IsNullOrEmpty(number))
            return Results.BadRequest(new { error = "partnerNumber is required." });

        var partner = await db.Partners
            .FirstOrDefaultAsync(p => p.PartnerNumber == number && p.DeletedAt == null, ct);
        if (partner is null) return Results.NotFound();

        partner.ImportDirectAdmission = body.DirectAdmission;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { partnerNumber = partner.PartnerNumber, directAdmission = partner.ImportDirectAdmission });
    }

    private static object RowResult(RowPlan p, string action, string message, string? studentNumber) => new
    {
        row = p.RowNumber,
        studentNumber = studentNumber ?? p.GivenStudentNumber ?? "(auto)",
        student = p.DisplayName,
        partner = p.Partner?.Name,
        programme = p.ProgrammeName,
        specialization = p.SpecializationName,
        action,
        message,
    };

    // ── Planning / validation (shared by validate + import) ────────────────

    private sealed class RowPlan
    {
        public int RowNumber;
        public Dictionary<string, string> Cells = new(StringComparer.OrdinalIgnoreCase);
        public readonly List<string> Errors = new();
        public string? SkipReason;
        public PartnerEntity? Partner;
        public Student? ExistingStudent;
        public bool ReusesEarlierRow;
        /// <summary>Set when the row's email already belongs to a NON-student,
        /// non-privileged account: instead of erroring, a student is attached
        /// to this existing account (adds the Student role, keeps the login).</summary>
        public ApplicationUser? AttachUser;
        public bool WasAttached;
        public string NewKey = "";
        public string? GivenStudentNumber;
        /// <summary>Unknown Student ID on an email-matched student — attached
        /// as an alias identifier at commit (label "CSV import").</summary>
        public string? AddAliasNumber;
        public Guid ProgrammeId;
        public string? Email;
        public string? DisplayName;
        public Guid SpecializationId;
        public string? ProgrammeName;
        public string? SpecializationName;
        public int ModeOfStudyId = 1;
        public DateTime? CommencementDate;
        public int? DurationMonths;
        public string? InstructionLanguage;
        public DateTime? DateOfBirth;
        public int? NationalityId;
        public Guid? PositionFunctionId;
        public Guid? EmploymentIndustryId;
        public Guid? CurrencyId;
        public decimal? SalaryAmount;
        public int YearsWork;
        public bool WantsCard = true;

        public string? Cell(string col)
        {
            var v = Cells.GetValueOrDefault(col)?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }
    }

    /// <summary>Loads the partner a scoped upload targets (?partnerId= on the
    /// admin routes). Null partner + null fail = unscoped (multi-partner CSV).</summary>
    private static async Task<(PartnerEntity? Partner, IResult? Fail)> ResolveForcedPartnerAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var raw = httpContext.Request.Query["partnerId"].ToString();
        if (string.IsNullOrEmpty(raw)) return (null, null);
        if (!Guid.TryParse(raw, out var pid))
            return (null, Results.BadRequest(new { error = "partnerId must be a GUID." }));
        var partner = await db.Partners
            .FirstOrDefaultAsync(p => p.PartnerId == pid && p.DeletedAt == null, ct);
        return partner is null ? (null, Results.NotFound()) : (partner, null);
    }

    private static async Task<(List<RowPlan> Plans, List<object> Partners, string? FileError)>
        BuildPlansAsync(OdinDbContext db, UserManager<ApplicationUser> userManager,
            HttpRequest request, PartnerEntity? forcedPartner, CancellationToken ct)
    {
        if (!request.HasFormContentType)
            return ([], [], "Upload the CSV as multipart form data (field name \"file\").");
        var form = await request.ReadFormAsync(ct);
        var file = form.Files["file"] ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            return ([], [], "No CSV file was uploaded.");

        string text;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
            text = await reader.ReadToEndAsync(ct);

        var raw = ParseCsv(text);
        if (raw.Count < 2)
            return ([], [], "The file has no data rows (expecting a header line plus at least one row).");

        var header = raw[0].Select(h => h.Trim()).ToArray();
        var known = Columns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = header.Where(h => h.Length > 0 && !known.Contains(h)).ToList();
        if (unknown.Count > 0)
            return ([], [], $"Unknown column(s): {string.Join(", ", unknown)}. Download the sample file for the expected layout.");
        var requiredColumns = forcedPartner is null
            ? new[] { "PartnerNumber", "ProgrammeCode", "SpecializationCode" }
            : new[] { "ProgrammeCode", "SpecializationCode" };
        foreach (var required in requiredColumns)
            if (!header.Contains(required, StringComparer.OrdinalIgnoreCase))
                return ([], [], $"Missing required column: {required}.");

        var plans = new List<RowPlan>();
        for (var i = 1; i < raw.Count; i++)
        {
            if (raw[i].All(string.IsNullOrWhiteSpace)) continue;
            var plan = new RowPlan { RowNumber = i + 1 };
            for (var c = 0; c < header.Length && c < raw[i].Length; c++)
                if (header[c].Length > 0) plan.Cells[header[c]] = raw[i][c];
            plans.Add(plan);
        }
        if (plans.Count == 0) return ([], [], "The file has no data rows.");

        // Preload lookups touched by the file.
        var partnersByNumber = (await db.Partners.Where(p => p.DeletedAt == null).ToListAsync(ct))
            .ToDictionary(p => p.PartnerNumber, p => p, StringComparer.OrdinalIgnoreCase);

        var programmeCodes = plans.Select(p => p.Cell("ProgrammeCode")?.ToLowerInvariant())
            .Where(c => c != null).Distinct().Cast<string>().ToList();
        var programmes = await db.Programmes
            .Where(p => p.DeletedAt == null && programmeCodes.Contains(p.Code.ToLower()))
            .Select(p => new { p.ProgrammeId, p.Code, p.Name, p.OwnerId, p.MinDurationMonths, p.MaxDurationMonths })
            .ToListAsync(ct);
        // Codes can collide case-insensitively ("MBA-La Xenia" vs
        // "MBA-LA XENIA" are distinct programmes) — keep every candidate and
        // resolve per row: exact-case match wins, otherwise a single
        // case-insensitive hit; several hits become a row error.
        var programmesByCode = programmes
            .GroupBy(p => p.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        var programmeIds = programmes.Select(p => p.ProgrammeId).ToList();

        var specs = await db.Specializations
            .Where(s => s.DeletedAt == null && programmeIds.Contains(s.ProgrammeId))
            .Select(s => new { s.SpecializationId, s.ProgrammeId, s.Code, s.Name })
            .ToListAsync(ct);
        var grants = (await db.ProgrammePartners
                .Where(pp => programmeIds.Contains(pp.ProgrammeId) && pp.IsActive != null)
                .Select(pp => new { pp.ProgrammeId, pp.PartnerId })
                .ToListAsync(ct))
            .Select(x => (x.ProgrammeId, x.PartnerId)).ToHashSet();

        var modes = await db.ModesOfStudy.Where(m => m.DeletedAt == null).ToListAsync(ct);
        var nationalities = await db.Nationalities.Where(n => n.DeletedAt == null).ToListAsync(ct);
        var currencies = await db.Currencies.ToListAsync(ct);
        var positionFunctions = await db.PositionFunctions.ToListAsync(ct);
        var industries = await db.EmploymentIndustries.ToListAsync(ct);

        var numbersInFile = plans.Select(p => p.Cell("StudentNumber"))
            .Where(n => n != null).Distinct().Cast<string>().ToList();
        var partnerNumbersById = partnersByNumber.Values.ToDictionary(p => p.PartnerId, p => p.PartnerNumber);

        // Matching is by EMAIL ONLY. Student numbers are used solely to
        // detect conflicts and to attach new alias IDs.
        var emailsInFile = plans.Select(p => p.Cell("Email")?.ToLowerInvariant())
            .Where(e => e != null).Distinct().Cast<string>().ToList();
        var studentsByEmail = (await db.Students
                .Where(s => s.DeletedAt == null && s.User.Email != null && emailsInFile.Contains(s.User.Email.ToLower()))
                .Select(s => new { Student = s, Email = s.User.Email!.ToLower() })
                .ToListAsync(ct))
            .GroupBy(x => x.Email)
            .ToDictionary(g => g.Key, g => g.First().Student, StringComparer.OrdinalIgnoreCase);

        // Which student (if any) owns each number in the file — primary
        // numbers and alias identifiers alike.
        var identifierOwners = (await db.StudentIdentifiers
                .Where(i => numbersInFile.Contains(i.Value))
                .Select(i => new { i.Value, i.StudentId })
                .ToListAsync(ct))
            .GroupBy(x => x.Value)
            .ToDictionary(g => g.Key, g => g.First().StudentId, StringComparer.OrdinalIgnoreCase);
        foreach (var srow in await db.Students
            .Where(st => st.DeletedAt == null && numbersInFile.Contains(st.StudentNumber))
            .Select(st => new { st.StudentNumber, st.StudentId }).ToListAsync(ct))
            identifierOwners.TryAdd(srow.StudentNumber, srow.StudentId);

        // Duplicate detection is PROGRAMME-level: a student already in a
        // programme (any specialization, any partner) is not re-added.
        var existingIds = studentsByEmail.Values.Select(s => s.StudentId).ToList();
        var existingEnrolments = (await db.Enrollments
                .Where(e => e.DeletedAt == null && existingIds.Contains(e.StudentId))
                .Select(e => new { e.StudentId, e.Specialization.ProgrammeId })
                .ToListAsync(ct))
            .Select(x => (x.StudentId, x.ProgrammeId)).ToHashSet();

        var newEmails = plans
            .Select(p => p.Cell("Email")?.ToLowerInvariant())
            .Where(e => e != null && !studentsByEmail.ContainsKey(e!))
            .Distinct().Cast<string>().ToList();
        // Emails in the file that already own a non-student account. Split them:
        // privileged accounts (admin/partner/staff) still can't be imported;
        // plain accounts get a student attached to the existing login instead
        // of erroring.
        var takenUsers = await db.Users
            .Where(u => (u.Email != null && newEmails.Contains(u.Email.ToLower()))
                     || (u.UserName != null && newEmails.Contains(u.UserName.ToLower())))
            .ToListAsync(ct);
        var privilegedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "SuperAdministrator", "Administrator", "Manager",
            "Editor", "Viewer", "Sales", "Partner",
        };
        var attachableUsers = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase);
        var privilegedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in takenUsers)
        {
            var key = u.Email?.ToLowerInvariant() ?? u.UserName?.ToLowerInvariant();
            if (key is null) continue;
            var roles = await userManager.GetRolesAsync(u);
            if (roles.Any(r => privilegedRoles.Contains(r))) privilegedEmails.Add(key);
            else attachableUsers[key] = u;
        }

        // Per-file state: identity of new students planned by earlier rows, and
        // (student key, specialization) pairs to catch duplicate lines.
        var plannedNew = new Dictionary<string, (Guid PartnerId, int Row)>(StringComparer.OrdinalIgnoreCase);
        var plannedEnrolments = new HashSet<(string Key, Guid SpecId)>();

        foreach (var plan in plans)
        {
            // Partner. Scoped uploads force the surrounding partner; a filled-in
            // PartnerNumber cell must then match (or be absent entirely).
            var partnerNumber = plan.Cell("PartnerNumber");
            if (forcedPartner is not null)
            {
                if (partnerNumber is not null && !string.Equals(
                        partnerNumber, forcedPartner.PartnerNumber, StringComparison.OrdinalIgnoreCase))
                    plan.Errors.Add($"This import belongs to partner '{forcedPartner.Name}'; " +
                        "leave the PartnerNumber cell blank or remove the column.");
                else plan.Partner = forcedPartner;
                partnerNumber = forcedPartner.PartnerNumber;
            }
            else if (partnerNumber is null) plan.Errors.Add("PartnerNumber is required.");
            else if (!partnersByNumber.TryGetValue(partnerNumber, out var partner))
                plan.Errors.Add($"Unknown PartnerNumber '{partnerNumber}'.");
            else plan.Partner = partner;

            // Programme + specialization + availability.
            var progCode = plan.Cell("ProgrammeCode");
            var specCode = plan.Cell("SpecializationCode");
            if (progCode is null) plan.Errors.Add("ProgrammeCode is required.");
            if (specCode is null) plan.Errors.Add("SpecializationCode is required.");
            if (progCode is not null && specCode is not null)
            {
                programmesByCode.TryGetValue(progCode, out var progCandidates);
                var prog = progCandidates?.Count == 1
                    ? progCandidates[0]
                    : progCandidates?.FirstOrDefault(p => string.Equals(p.Code, progCode, StringComparison.Ordinal));
                if (progCandidates is null)
                    plan.Errors.Add($"Unknown ProgrammeCode '{progCode}'.");
                else if (prog is null)
                    plan.Errors.Add($"ProgrammeCode '{progCode}' is ambiguous — several programmes share it apart from letter casing ({string.Join(", ", progCandidates.Select(p => $"'{p.Code}'"))}); type it EXACTLY as shown.");
                else
                {
                    plan.ProgrammeName = prog.Name;
                    plan.ProgrammeId = prog.ProgrammeId;
                    var spec = specs.FirstOrDefault(s => s.ProgrammeId == prog.ProgrammeId
                        && string.Equals(s.Code, specCode, StringComparison.OrdinalIgnoreCase));
                    if (spec is null)
                        plan.Errors.Add($"Programme '{progCode}' has no specialization with code '{specCode}'.");
                    else
                    {
                        plan.SpecializationId = spec.SpecializationId;
                        plan.SpecializationName = spec.Name;
                    }
                    if (plan.Partner is not null
                        && prog.OwnerId != null
                        && prog.OwnerId != plan.Partner.PartnerId
                        && !grants.Contains((prog.ProgrammeId, plan.Partner.PartnerId)))
                        plan.Errors.Add($"Programme '{progCode}' isn't available to partner '{partnerNumber}'.");
                    if (plan.DurationMonths is { } m && prog.MaxDurationMonths > 0
                        && (m < prog.MinDurationMonths || m > prog.MaxDurationMonths))
                        plan.Errors.Add($"DurationOfStudyMonths {m} is outside programme '{progCode}''s " +
                            $"allowed range ({prog.MinDurationMonths}-{prog.MaxDurationMonths}).");
                }
            }

            // Per-enrolment extras: parsed for EVERY line, also ones that only
            // add a programme to an existing student.
            if (plan.Cell("CommencementDate") is { } comm)
            {
                if (DateTime.TryParseExact(comm, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var cd))
                    plan.CommencementDate = DateTime.SpecifyKind(cd, DateTimeKind.Utc);
                else plan.Errors.Add($"CommencementDate '{comm}' isn't a valid date (use yyyy-MM-dd).");
            }
            if (plan.Cell("DurationOfStudyMonths") is { } dur)
            {
                if (int.TryParse(dur, out var months) && months > 0) plan.DurationMonths = months;
                else plan.Errors.Add($"DurationOfStudyMonths '{dur}' isn't a positive whole number.");
            }
            plan.InstructionLanguage = plan.Cell("InstructionLanguage");

            // Mode of study: id or name; blank falls back to 1.
            var modeRaw = plan.Cell("ModeOfStudy");
            if (modeRaw is not null)
            {
                var mode = int.TryParse(modeRaw, out var modeId)
                    ? modes.FirstOrDefault(m => m.ModeOfStudyId == modeId)
                    : modes.FirstOrDefault(m => string.Equals(m.Name, modeRaw, StringComparison.OrdinalIgnoreCase));
                if (mode is null) plan.Errors.Add($"Unknown ModeOfStudy '{modeRaw}'.");
                else plan.ModeOfStudyId = mode.ModeOfStudyId;
            }

            // Student identity — EMAIL is the only match key.
            plan.GivenStudentNumber = plan.Cell("StudentNumber");
            plan.Email = plan.Cell("Email")?.ToLowerInvariant();
            if (plan.Email is null)
            {
                plan.Errors.Add("Email is required — rows are matched by email only.");
                plan.NewKey = $"row-{plan.RowNumber}";
                plan.DisplayName = $"{plan.Cell("FirstName")} {plan.Cell("LastName")}".Trim();
            }
            else if (studentsByEmail.TryGetValue(plan.Email, out var existing))
            {
                plan.ExistingStudent = existing;
                plan.NewKey = plan.Email;
                plan.DisplayName = existing.StudentNumber;
                // Student ID column on a matched row: known ID → nothing to
                // do; someone ELSE's ID → the whole row is rejected; unknown
                // ID → attached as a new alias identifier.
                if (plan.GivenStudentNumber is { } num)
                {
                    if (identifierOwners.TryGetValue(num, out var ownerId))
                    {
                        if (ownerId != existing.StudentId)
                            plan.Errors.Add($"StudentNumber '{num}' already belongs to a different student — row rejected.");
                    }
                    else
                    {
                        plan.AddAliasNumber = num;
                    }
                }
            }
            else
            {
                // New student: personal columns apply. A given StudentNumber
                // becomes the primary ID; blank auto-generates one.
                plan.NewKey = plan.Email;
                plan.DisplayName = $"{plan.Cell("FirstName")} {plan.Cell("LastName")}".Trim();

                if (plan.GivenStudentNumber is { } num && identifierOwners.ContainsKey(num))
                    plan.Errors.Add($"StudentNumber '{num}' already belongs to another student — row rejected.");

                var isFirstRowForStudent = !plannedNew.ContainsKey(plan.NewKey);
                if (isFirstRowForStudent)
                {
                    if (plan.Cell("FirstName") is null) plan.Errors.Add("FirstName is required for a new student.");
                    if (plan.Cell("LastName") is null) plan.Errors.Add("LastName is required for a new student.");
                    if (privilegedEmails.Contains(plan.Email))
                        plan.Errors.Add($"Email '{plan.Email}' belongs to a staff/partner account and cannot be imported as a student.");
                    else if (attachableUsers.TryGetValue(plan.Email, out var attachUser))
                        // Reuse the existing plain account: a student will be
                        // attached to it rather than a new account created.
                        plan.AttachUser = attachUser;

                    ParsePersonalFields(plan, nationalities, currencies, positionFunctions, industries);

                    if (plan.Partner is not null && plan.Errors.Count == 0)
                        plannedNew[plan.NewKey] = (plan.Partner.PartnerId, plan.RowNumber);
                }
                else
                {
                    plan.ReusesEarlierRow = true;
                    if (plan.Partner is not null && plannedNew[plan.NewKey].PartnerId != plan.Partner.PartnerId)
                        plan.Errors.Add($"Row {plannedNew[plan.NewKey].Row} creates this student under a " +
                            "different partner; all rows for one student must use the same PartnerNumber.");
                }
            }

            // Duplicate detection is programme-level: a programme the student
            // already has (any spec, any partner) is skipped — the row's new
            // Student ID is still attached.
            if (plan.Errors.Count == 0 && plan.ProgrammeId != Guid.Empty)
            {
                if (plan.ExistingStudent is not null
                    && existingEnrolments.Contains((plan.ExistingStudent.StudentId, plan.ProgrammeId)))
                    plan.SkipReason = "Already in this programme — programme skipped.";
                else if (!plannedEnrolments.Add((plan.NewKey, plan.ProgrammeId)))
                    plan.SkipReason = "Duplicate line: same student and programme appears earlier in the file.";
            }
        }

        var involvedPartners = plans.Where(p => p.Partner is not null)
            .Select(p => p.Partner!).DistinctBy(p => p.PartnerId)
            .Select(p => (object)new
            {
                partnerNumber = p.PartnerNumber,
                name = p.Name,
                directAdmission = p.ImportDirectAdmission,
            }).ToList();

        return (plans, involvedPartners, null);
    }

    private static void ParsePersonalFields(
        RowPlan plan,
        List<Nationality> nationalities,
        List<SharedLibrary.Basics.Opaque.Domains.Payments.Currency> currencies,
        List<PositionFunction> positionFunctions,
        List<EmploymentIndustry> industries)
    {
        if (plan.Cell("DateOfBirth") is { } dob)
        {
            if (DateTime.TryParseExact(dob, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var d))
                plan.DateOfBirth = DateTime.SpecifyKind(d, DateTimeKind.Utc);
            else plan.Errors.Add($"DateOfBirth '{dob}' isn't a valid date (use yyyy-MM-dd).");
        }

        if (plan.Cell("NationalityCode") is { } nat)
        {
            var match = nationalities.FirstOrDefault(n =>
                string.Equals(n.Code, nat, StringComparison.OrdinalIgnoreCase)
                || string.Equals(n.Name, nat, StringComparison.OrdinalIgnoreCase));
            if (match is null) plan.Errors.Add($"Unknown NationalityCode '{nat}' (use the ISO-2 code, e.g. DK).");
            else plan.NationalityId = match.NationalityId;
        }

        if (plan.Cell("YearsWorkExperience") is { } years)
        {
            if (int.TryParse(years, out var y) && y >= 0) plan.YearsWork = y;
            else plan.Errors.Add($"YearsWorkExperience '{years}' isn't a whole number.");
        }

        if (plan.Cell("PositionFunction") is { } pos)
        {
            var match = positionFunctions.FirstOrDefault(p =>
                string.Equals(p.Name, pos, StringComparison.OrdinalIgnoreCase));
            if (match is null) plan.Errors.Add($"Unknown PositionFunction '{pos}'.");
            else plan.PositionFunctionId = match.PositionFunctionId;
        }

        if (plan.Cell("EmploymentIndustry") is { } ind)
        {
            var match = industries.FirstOrDefault(x =>
                string.Equals(x.Name, ind, StringComparison.OrdinalIgnoreCase));
            if (match is null) plan.Errors.Add($"Unknown EmploymentIndustry '{ind}'.");
            else plan.EmploymentIndustryId = match.EmploymentIndustryId;
        }

        if (plan.Cell("MonthlySalaryAmount") is { } salary)
        {
            if (decimal.TryParse(salary, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                && amount >= 0)
                plan.SalaryAmount = amount;
            else plan.Errors.Add($"MonthlySalaryAmount '{salary}' isn't a number.");
        }

        if (plan.Cell("MonthlySalaryCurrency") is { } cur)
        {
            var match = currencies.FirstOrDefault(c =>
                string.Equals(c.Code, cur, StringComparison.OrdinalIgnoreCase));
            if (match is null) plan.Errors.Add($"Unknown MonthlySalaryCurrency '{cur}' (use the code, e.g. USD).");
            else plan.CurrencyId = match.CurrencyId;
        }

        if (plan.Cell("WantsStudentCard") is { } card)
        {
            var v = card.ToLowerInvariant();
            if (v is "true" or "yes" or "1") plan.WantsCard = true;
            else if (v is "false" or "no" or "0") plan.WantsCard = false;
            else plan.Errors.Add($"WantsStudentCard '{card}' isn't true/false.");
        }
    }

    private static string CsvEscape(string value) => ImportCsvHelpers.CsvEscape(value);

    private static List<string[]> ParseCsv(string text) => ImportCsvHelpers.ParseCsv(text);
}
