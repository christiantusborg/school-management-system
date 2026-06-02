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

    public DateTime? DeletedAt { get; set; }
   
    public Guid? OwnerId { get; set; }
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