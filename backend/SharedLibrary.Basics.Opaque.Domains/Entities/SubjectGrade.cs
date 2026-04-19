using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class SubjectGrade : IDeletedAtEntity
{
    public Guid SubjectGradeId { get; set; } = Guid.NewGuid();
    public Guid StudentEnrollmentId { get; set; }
    public StudentEnrollment StudentEnrollment { get; set; } = default!;
    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = default!;
    public decimal? Score { get; set; }
    public DateTime? GradedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
