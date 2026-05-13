using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace SharedLibrary.Basics.Opaque.Domains;

public class SubjectGrade : IEntity
{
    public Guid SubjectGradeId { get; set; } = Guid.NewGuid();
    public int Score { get; set; }
    public DateTime? GradedAt { get; set; }

    public Guid StudentEnrollmentId { get; set; }
    public Guid SubjectId { get; set; }
    
    public Subject Subject { get; set; } = default!;
    public Enrollment Enrollment { get; set; } = default!;
}