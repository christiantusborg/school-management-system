using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class InviteCode : IEntity
{
    public Guid InviteCodeId { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;
    public string CreatedByUserId { get; set; } = default!;
    public ApplicationUser CreatedByUser { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public string? RedeemedByUserId { get; set; }
    public ApplicationUser? RedeemedByUser { get; set; }
    public string AssignedRole { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
}
