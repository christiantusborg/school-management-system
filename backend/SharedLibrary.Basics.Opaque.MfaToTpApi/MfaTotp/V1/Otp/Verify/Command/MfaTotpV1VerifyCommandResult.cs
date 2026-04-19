using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Command;

public sealed class MfaTotpV1VerifyCommandResult : IMfaTotpV1VerifyCommandResultQueue
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
