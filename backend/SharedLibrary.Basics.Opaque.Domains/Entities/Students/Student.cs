using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Student
    : IDeletedAtEntity
{
    public Guid StudentId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;

    public string StudentNumber { get; set; } = default!;

    /// <summary>
    /// True for students migrated from the old system. Their Student ID was
    /// assigned externally and set manually by the Admission Office, and they
    /// skip the offer/admission letter flow. Defaults false for new applicants.
    /// </summary>
    public bool IsLegacyStudent { get; set; }

    /// <summary>
    /// Whether this student is enabled in Moodle (the LMS). Toggled by the
    /// Admission Office on the student detail modal's Moodle tab. Defaults to
    /// false; no automatic Moodle integration is wired yet.
    /// </summary>
    public bool MoodleEnabled { get; set; }

    /// <summary>Moodle (LMS) login username for this student, set by the
    /// Admission Office on the Moodle tab. Null until entered.</summary>
    public string? MoodleUsername { get; set; }

    /// <summary>Moodle (LMS) login password for this student. Stored as-is so
    /// the Admission Office can view and hand it to the student; not the app's
    /// own auth credential.</summary>
    public string? MoodlePassword { get; set; }

    public Guid PartnerId { get; set; }
    public string? PassportId { get; set; }

    /// <summary>Overrides the real StudentNumber ON THE STUDENT ID CARD only
    /// (the [student number] tag when rendering the IDCARD letter). Editable
    /// by both Admission and the partner; blank = card shows StudentNumber.</summary>
    public string? StudentCardId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? HighestDegree { get; set; }

    /// <summary>Free-text specialization of the highest degree / previous
    /// education (e.g. "Marketing", "Mechanical Engineering"). Entered on the
    /// signup Background step; editable by the Admission Office.</summary>
    public string? DegreeSpecialization { get; set; }

    public string? LanguageResult { get; set; }
    public int YearsWorkExperience { get; set; }
    /// <summary>Highest signup-wizard step the applicant has completed (0 = not started, 6 = submitted).</summary>
    public int WizardStep { get; set; }

    // Identity / address (filled by the wizard)
    public int? NationalityId { get; set; }

    /// <summary>Self-declared gender: Female, Male, Another gender identity,
    /// Prefer not to say. Free-text so the option list can evolve.</summary>
    public string? Gender { get; set; }

    /// <summary>Signup-wizard opt-in for the digital student ID card. Default
    /// true so admin-created students still get cards; the wizard's
    /// "Yes, I would like a digital student card" checkbox can clear it.</summary>
    public bool WantsStudentIdCard { get; set; } = true;

    /// <summary>Optional disability/learning-difference disclosure: Yes, No,
    /// Prefer not to say. Null until the applicant answers.</summary>
    public string? DisabilityDisclosure { get; set; }

    /// <summary>Free-text description of the support or reasonable adjustments
    /// the applicant would find helpful. Only meaningful when
    /// <see cref="DisabilityDisclosure"/> is "Yes".</summary>
    public string? DisabilitySupportNeeds { get; set; }

    // ── Professional background (wizard Background step) ──
    /// <summary>Current position by function (FK to the configurable
    /// <see cref="PositionFunction"/> list).</summary>
    public Guid? CurrentPositionFunctionId { get; set; }
    public PositionFunction? CurrentPositionFunction { get; set; }

    /// <summary>Current employment industry (FK to the configurable
    /// <see cref="EmploymentIndustry"/> list).</summary>
    public Guid? CurrentEmploymentIndustryId { get; set; }
    public EmploymentIndustry? CurrentEmploymentIndustry { get; set; }

    /// <summary>Monthly salary at the time of starting the education.</summary>
    public decimal? MonthlySalaryAmount { get; set; }
    /// <summary>Currency of <see cref="MonthlySalaryAmount"/> (FK to the shared
    /// payment <see cref="Payments.Currency"/> list).</summary>
    public Guid? MonthlySalaryCurrencyId { get; set; }
    public Payments.Currency? MonthlySalaryCurrency { get; set; }


    public DateTime? DeletedAt { get; set; }

    
    public ICollection<StudentDocument> Documents { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = [];
    public ICollection<StudentNote> Notes { get; set; } = [];
    public ICollection<UserLanguage> Languages { get; set; } = [];
    
    public Nationality? Nationality { get; set; }
    public ApplicationUser User { get; set; } = default!;
}


