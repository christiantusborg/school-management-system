using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public class Fido2Credential : IEntity
{
    public Guid Fido2CredentialId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public byte[] CredentialId { get; set; } = null!;
    public byte[] PublicKey { get; set; } = null!;
    public uint SignatureCounter { get; set; }
    public Guid AaGuid { get; set; }
    public string? Transports { get; set; }
    public string Label { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public ApplicationUser User { get; set; } = null!;
}
