using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class UserContactEmail : IEntity
{
    public Guid UserContactEmailId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Label { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public ApplicationUser User { get; set; } = null!;
}
