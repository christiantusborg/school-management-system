using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains.Partners;

public class PartnerUsers : IDeletedAtEntity
{
    public Guid PartnerId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;

    public DateTime? DeletedAt { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = default!;

}



    