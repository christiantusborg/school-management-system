using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class PartnerContactPhone : IDeletedAtEntity
{
    public Guid PartnerContactPhoneId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }

    public string? Phone { get; set; }
    public bool IsPrimary { get; set; }

    public DateTime? DeletedAt { get; set; }
    
    public Partner Partner { get; set; } = default!;
}