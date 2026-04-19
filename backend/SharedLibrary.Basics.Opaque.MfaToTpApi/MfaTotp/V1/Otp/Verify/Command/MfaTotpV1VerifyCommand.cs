namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Command;

public sealed class MfaTotpV1VerifyCommand : IHandleableCommand<
    MfaTotpV1VerifyCommand,
    MfaTotpV1VerifyCommandValidator,
    MfaTotpV1VerifyCommandHandler,
    MfaTotpV1VerifyCommandResult>
{
    public required string PendingId { get; init; }
    public required string Code { get; init; }
}
