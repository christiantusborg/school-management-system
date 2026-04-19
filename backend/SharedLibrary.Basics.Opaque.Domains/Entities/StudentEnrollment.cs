using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class StudentEnrollment : IDeletedAtEntity
{
    public Guid StudentEnrollmentId { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = default!;
    public Guid ProgrammeId { get; set; }
    public Programme Programme { get; set; } = default!;
    public Guid MajorId { get; set; }
    public Major Major { get; set; } = default!;
    public DateTime? CommencementDate { get; set; }
    public int ModeOfStudyId { get; set; }
    public ModeOfStudy ModeOfStudy { get; set; } = default!;
    public int? DurationOfStudyMonths { get; set; }
    public int? PathwayId { get; set; }
    public Pathway? Pathway { get; set; }
    public string? OfferType { get; set; }
    public DateTime? PaymentDoneAt { get; set; }
    public DateTime? AdmissionConfirmedAt { get; set; }
    public bool MissingDocsSubmitted { get; set; }
    public bool CertReleased { get; set; }
    public int EnrollmentStatusId { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; } = default!;
    public DateTime? TranscriptReleasedAt { get; set; }
    public int TuitionFeeStatusId { get; set; }
    public TuitionFeeStatus TuitionFeeStatus { get; set; } = default!;
    public string? OtherFeesStatus { get; set; }
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public DateTime? DeletedAt { get; set; }

    public FinalProject? FinalProject { get; set; }
    public ICollection<SubjectGrade> SubjectGrades { get; set; } = [];
    public ICollection<EnrollmentDocument> EnrollmentDocuments { get; set; } = [];
}
