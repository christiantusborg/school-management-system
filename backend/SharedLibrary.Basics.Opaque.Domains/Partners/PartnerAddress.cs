using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class PartnerAddress : IDeletedAtEntity
{
    public Guid PartnerAddressId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }
    
    public Guid PartnerAddressTypeId { get; set; }
    public PartnerAddressType Type { get; set; } = default!;

    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? StateRegion { get; set; }
    public string? PostalCode { get; set; }
    public string CountryCode { get; set; } = default!; // ISO 3166-1 alpha-2

    public DateTime? DeletedAt { get; set; }
    
    public Partner Partner { get; set; } = default!;
}