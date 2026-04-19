using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Command;

public sealed class MfaEmailV1VerifyCommandResult : IMfaEmailV1VerifyCommandResultQueue
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
