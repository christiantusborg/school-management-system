using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class OpaqueCredential : IEntity
{
    public Guid OpaqueCredentialId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public byte[] OprfSeed { get; set; } = null!;
    public byte[] ClientPublicKey { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;

    public ApplicationUser User { get; set; } = null!;
}
