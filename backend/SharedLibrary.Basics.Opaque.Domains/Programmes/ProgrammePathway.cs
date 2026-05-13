using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace SharedLibrary.Basics.Opaque.Domains;

public class ProgrammePathway : IDeletedAtEntity
{
    public Guid ProgrammePathwayId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public Guid PathwayId { get; set; }
    
    public DateTime? DeletedAt { get; set; }
    
    public Pathway Pathway { get; set; } = default!;
    public Programme Programme { get; set; } = default!;
}
