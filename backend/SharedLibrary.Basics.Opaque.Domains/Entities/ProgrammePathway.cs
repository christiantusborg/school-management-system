using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class ProgrammePathway : IDeletedAtEntity
{
    public Guid ProgrammePathwayId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public Programme Programme { get; set; } = default!;
    public int PathwayId { get; set; }
    public Pathway Pathway { get; set; } = default!;
    public DateTime? DeletedAt { get; set; }
}
