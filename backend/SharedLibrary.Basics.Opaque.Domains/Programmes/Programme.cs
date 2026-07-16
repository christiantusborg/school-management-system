using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public class Programme : IDeletedAtEntity
{
    public Guid ProgrammeId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;

    /// <summary>
    /// Allowed range (in months) for any enrolment under this programme.
    /// The specialisation's <see cref="Specialization.DurationOfStudyMonths"/>
    /// continues to act as the default within this range; the partner picks
    /// the actual approved value per enrolment during the review flow and
    /// stores it on <see cref="Enrollment.ApprovedDurationMonths"/>.
    /// </summary>
    public int MinDurationMonths { get; set; }
    public int MaxDurationMonths { get; set; }

    /// <summary>
    /// ECTS credits a student must complete before the enrolment can be
    /// submitted for grading / graduation. The partner sets it when creating a
    /// custom programme; Admission can edit it at any time. Null = no threshold
    /// (grade submission is not gated). Compared against the sum of ECTS for the
    /// subjects that have a score entered.
    /// </summary>
    public decimal? RequiredEcts { get; set; }

    /// <summary>When true, a Digital Student Card letter can be authored and
    /// generated for enrolments under this programme; the card row is hidden
    /// everywhere while off.</summary>
    public bool IssueDigitalStudentCard { get; set; }

    public DateTime? DeletedAt { get; set; }
   
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// The awarding school this programme belongs to. Required; shown as a
    /// suffix on the programme name in the portal UI. See <see cref="School"/>.
    /// </summary>
    public Guid? SchoolId { get; set; }
    public School? School { get; set; }

    public ICollection<Specialization> Specializations { get; set; } = [];
    public ICollection<ProgrammeDocumentRequirement> RequiredDocumentTypes { get; set; } = [];
    public Partner Owner { get; set; }

    /// <summary>
    /// Education level this programme awards on completion (e.g. BBA → Bachelor's,
    /// MBA → Master's, DBA → Doctorate). Used by the signup wizard to chain
    /// pathway eligibility across multi-degree applications.
    /// </summary>
    public Guid? AwardEducationLevelId { get; set; }
    public EducationLevel? AwardEducationLevel { get; set; }
}