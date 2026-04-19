using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class UserProfile : IEntity
{
    public Guid UserProfileId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
}
