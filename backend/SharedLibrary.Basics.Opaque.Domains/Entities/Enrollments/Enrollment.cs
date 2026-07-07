using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using SharedLibrary.Basics.Opaque.Domains.Partners;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Enrollment : IDeletedAtEntity
{
    public Guid StudentEnrollmentId { get; set; } = Guid.NewGuid();
    public DateTime? CommencementDate { get; set; }

    /// <summary>
    /// Current lifecycle state. Replaces the seven nullable timestamps and the
    /// old <c>AdditionalStatueId</c> FK. Transitions are recorded in
    /// <see cref="EnrollmentStatusNote"/> (one row per change with
    /// <c>StatusId</c>, <c>ByUserId</c>, <c>CreatedAt</c>).
    /// </summary>
    public Guid StatusId { get; set; }

    public int ModeOfStudyId { get; set; }
    public int PathwayId { get; set; }
    public Guid SpecializationId { get; set; }
    public Guid StudentId { get; set; }
    public Guid PartnerId { get; set; }
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Duration in months the partner agreed to during the review flow.
    /// Must fall between <see cref="Programme.MinDurationMonths"/> and
    /// <see cref="Programme.MaxDurationMonths"/>. Null means the partner
    /// hasn't picked one yet; consumers should fall back to
    /// <see cref="Specialization.DurationOfStudyMonths"/>.
    /// </summary>
    public int? ApprovedDurationMonths { get; set; }

    /// <summary>
    /// Per-enrolment override of the teaching/instruction language, set by admin
    /// or partner during review (or in the detail editor). When non-blank it
    /// wins over <see cref="Specialization.InstructionLanguage"/> in every
    /// letter/transcript (the [instruction language] tag). Null/blank = use the
    /// specialization's language.
    /// </summary>
    public string? InstructionLanguageOverride { get; set; }

    /// <summary>
    /// Admission-Office overrides for the date printed on (and the effective
    /// release date of) the offer / admission letters. When set, the letter's
    /// [date] tag uses this instead of the render date — e.g. to backdate an
    /// offer that was actually made earlier. Null = use the render date.
    /// </summary>
    public DateTime? OfferLetterDate { get; set; }
    public DateTime? AdmissionLetterDate { get; set; }
    public DateTime? TranscriptDate { get; set; }

    /// <summary>
    /// Stable per-enrolment reference code: the first 8 hex characters of a
    /// GUID, generated once on the first letter release and reused for every
    /// letter and every regeneration thereafter. Letters render it as
    /// <c>IBSS-{type}-{code}</c> (e.g. <c>IBSS-OL-1A2B3C4D</c> for the offer
    /// letter); the public verify endpoint looks an enrolment up by this code
    /// so a printed reference can be confirmed genuine. Null until the first
    /// letter is released.
    /// </summary>
    public string? LetterReferenceCode { get; set; }

    public Pathway? Pathway { get; set; } = default!;
    public ModeOfStudy ModeOfStudy { get; set; } = default!;
    public Specialization Specialization { get; set; } = default!;
    public Student Student { get; set; } = default!;
    public Partner Partner { get; set; } = default!;

    public EnrollmentStatus Status { get; set; } = default!;
    public ICollection<SubjectGrade> SubjectGrades { get; set; } = [];
}