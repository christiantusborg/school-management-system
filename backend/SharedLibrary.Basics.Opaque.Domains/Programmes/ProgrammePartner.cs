using SharedLibrary.Basics.Opaque.Domains.Partners;

namespace SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

public class ProgrammePartner
{
    public Guid ProgrammePartnerId { get; set; } = Guid.NewGuid();
    public Guid ProgrammeId { get; set; }
    public Guid PartnerId { get; set; }
    public DateTime? IsActive { get; set; }
    
    public Programme Programme { get; set; }
    public Partner Partner { get; set; }
}