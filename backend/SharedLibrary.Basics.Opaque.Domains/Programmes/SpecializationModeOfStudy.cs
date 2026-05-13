using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public class SpecializationModeOfStudy : IEntity
{
    public Guid SpecializationModeOfStudyId  { get; set; } = Guid.NewGuid();
    public Guid SpecializationId  { get; set; }
    public Guid ModeOfStudyId  { get; set; }
    
    public ICollection<ModeOfStudy> ModesOfStudy { get; set; } = new List<ModeOfStudy>();
    public ICollection<Specialization> Subjects { get; set; } = new List<Specialization>();
}