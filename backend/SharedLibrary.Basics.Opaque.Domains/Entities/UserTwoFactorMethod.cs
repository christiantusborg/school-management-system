using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace SharedLibrary.Basics.Opaque.Domains;

public enum MfaMethodType { Email = 0, Sms = 1, Totp = 2, Fido2 = 3 }

public class UserTwoFactorMethod : IEntity
{
    public Guid UserTwoFactorMethodId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public MfaMethodType MethodType { get; set; }
    public string? TotpSecret { get; set; }
    public long? LastTotpStepUsed { get; set; }
    public DateTime EnabledAt { get; set; } = DateTime.UtcNow;
    public Guid TenantId { get; set; } = TenantConstants.DefaultTenantId;
    public ApplicationUser User { get; set; } = null!;
}
