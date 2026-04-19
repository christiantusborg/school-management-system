namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Command;

public sealed class MfaEmailV1VerifyCommand : IHandleableCommand<
    MfaEmailV1VerifyCommand,
    MfaEmailV1VerifyCommandValidator,
    MfaEmailV1VerifyCommandHandler,
    MfaEmailV1VerifyCommandResult>
{
    public required string PendingId { get; init; }
    public required string Code { get; init; }
}
