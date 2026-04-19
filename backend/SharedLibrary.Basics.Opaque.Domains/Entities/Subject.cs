using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Subject : IDeletedAtEntity
{
    public Guid SubjectId { get; set; } = Guid.NewGuid();
    public Guid MajorId { get; set; }
    public Major Major { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Ects { get; set; }
    public DateTime? DeletedAt { get; set; }
}
