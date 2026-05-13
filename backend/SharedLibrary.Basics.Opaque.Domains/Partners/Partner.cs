using QuVian.SharedLibrary.Basics.Repositories.Interfaces;
using SharedLibrary.Basics.Opaque.Domains.PartnersProgrammes;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class Partner : IDeletedAtEntity
{
    public Guid PartnerId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;

    // Organisation identity
    public string? Website { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }

    /// <summary>
    /// Partner-level "disabled" flag. Distinct from <see cref="DeletedAt"/>:
    /// a disabled partner is invisible to its own users (they cannot log in)
    /// but still appears in the admin list and can be re-enabled. A deleted
    /// partner is soft-deleted via <see cref="DeletedAt"/> and only appears
    /// when the admin toggles "Show deleted". Null = enabled.
    /// </summary>
    public DateTime? DisabledAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation
    public ICollection<PartnerAddress> Addresses { get; set; } = new List<PartnerAddress>();
    public ICollection<PartnerContactPhone> Phones { get; set; } = new List<PartnerContactPhone>();
    public ICollection<PartnerContactEmail> Emails { get; set; } = new List<PartnerContactEmail>();
    public ICollection<PartnerContract> Contracts { get; set; } = new List<PartnerContract>();
    public ICollection<PartnerUsers> Users { get; set; } = new List<PartnerUsers>();
    public ICollection<Programme> Programmes { get; set; } = new List<Programme>();
}