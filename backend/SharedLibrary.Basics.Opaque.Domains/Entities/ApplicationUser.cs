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

    /// <summary>Partner-user variant: teachers get READ access to everything
    /// their partner sees, plus exactly two writes — saving grade drafts
    /// (never submitting) and commenting on uploaded assignments. Enforced
    /// centrally in RolePathGuardMiddleware.</summary>
    public bool IsTeacher { get; set; }
    public DateTime? DeletedAt { get; set; }
}
