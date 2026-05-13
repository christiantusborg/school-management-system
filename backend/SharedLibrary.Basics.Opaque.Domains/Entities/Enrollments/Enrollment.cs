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

    public Pathway? Pathway { get; set; } = default!;
    public ModeOfStudy ModeOfStudy { get; set; } = default!;
    public Specialization Specialization { get; set; } = default!;
    public Student Student { get; set; } = default!;
    public Partner Partner { get; set; } = default!;

    public EnrollmentStatus Status { get; set; } = default!;
    public ICollection<SubjectGrade> SubjectGrades { get; set; } = [];
}