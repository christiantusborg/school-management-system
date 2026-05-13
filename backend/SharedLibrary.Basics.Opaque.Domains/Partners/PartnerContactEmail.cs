using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class PartnerContactEmail : IDeletedAtEntity
{
    public Guid PartnerContactEmailId { get; set; } = Guid.NewGuid();
    public Guid PartnerId { get; set; }

    public string? Email { get; set; }
    public bool IsPrimary { get; set; }

    public DateTime? DeletedAt { get; set; }
    
    public Partner Partner { get; set; } = default!;
}