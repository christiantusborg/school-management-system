using Microsoft.AspNetCore.Identity;
using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class ApplicationUser : IdentityUser, IEntity
{
    public bool IsEnabled { get; set; } = true;
    public bool RecoveryCodesConfirmed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public Guid? PartnerId { get; set; }
}
