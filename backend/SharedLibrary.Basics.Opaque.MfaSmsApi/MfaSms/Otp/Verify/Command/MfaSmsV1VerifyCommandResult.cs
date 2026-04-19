using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Command;

public sealed class MfaSmsV1VerifyCommandResult : IMfaSmsV1VerifyCommandResultQueue
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
