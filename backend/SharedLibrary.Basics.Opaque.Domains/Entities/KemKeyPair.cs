using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class KemKeyPair : IEntity
{
    public string UserId { get; set; } = null!;
    public byte[] PublicKey { get; set; } = null!;
    public byte[] EncryptedPrivateKey { get; set; } = null!;
    public byte[] Nonce { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;

    public ApplicationUser User { get; set; } = null!;
}
