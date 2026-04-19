namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Command;

public sealed class MfaSmsV1VerifyCommand : IHandleableCommand<
    MfaSmsV1VerifyCommand,
    MfaSmsV1VerifyCommandValidator,
    MfaSmsV1VerifyCommandHandler,
    MfaSmsV1VerifyCommandResult>
{
    public required string PendingId { get; init; }
    public required string Code { get; init; }
}
