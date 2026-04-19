using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Major : IDeletedAtEntity
{
    public Guid MajorId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public Programme Programme { get; set; } = default!;
    public string Name { get; set; } = default!;
    public DateTime? DeletedAt { get; set; }

    public ICollection<Subject> Subjects { get; set; } = [];
}
