using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public class Subject : IDeletedAtEntity
{
    public Guid SubjectId { get; set; } = Guid.NewGuid();
    public Guid SpecializationId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int Ects { get; set; }
    
    public DateTime? IsActive { get; set; }
    public DateTime? DeletedAt { get; set; }
   
    public Specialization Specializations { get; set; }
}