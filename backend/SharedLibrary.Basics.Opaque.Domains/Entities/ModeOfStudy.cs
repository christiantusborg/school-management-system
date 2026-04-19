using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class ModeOfStudy : IDeletedAtEntity
{
    public int ModeOfStudyId { get; set; }
    public string Name { get; set; } = default!;
    public DateTime? DeletedAt { get; set; }
}
