using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class UserAddress : IEntity
{
    public Guid UserAddressId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public string? Label { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public bool IsPrimary { get; set; }
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public ApplicationUser User { get; set; } = null!;
}
