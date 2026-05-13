using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class StudentNote : IDeletedAtEntity
{
    public Guid StudentNoteId { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public Student Student { get; set; } = default!;
    public Guid? StudentEnrollmentId { get; set; }
    public Enrollment? StudentEnrollment { get; set; }
    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
