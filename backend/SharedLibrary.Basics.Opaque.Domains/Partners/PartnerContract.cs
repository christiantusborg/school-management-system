using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class PartnerContract : IDeletedAtEntity
{
    public Guid PartnerContractId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    public Partner Partner { get; set; } = default!;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public DateTime? DeletedAt { get; set; }

    public ICollection<PartnerContractNote> Notes { get; set; } = new List<PartnerContractNote>();
}