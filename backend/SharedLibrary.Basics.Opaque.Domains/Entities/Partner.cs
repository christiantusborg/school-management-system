using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Partner : IDeletedAtEntity
{
    public Guid PartnerId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public DateTime? DeletedAt { get; set; }

    // Contact person
    public string? ContactPersonName { get; set; }
    public string? ContactPersonTitle { get; set; }
    public string? ContactPersonEmail { get; set; }
    public string? ContactPersonPhone { get; set; }

    // Address
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateRegion { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Organisation
    public string? Website { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }

    // Partnership metadata
    public DateTime? ContractStart { get; set; }
    public DateTime? ContractEnd { get; set; }
    public string? Tier { get; set; }
    public string? InternalNotes { get; set; }
}
