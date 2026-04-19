namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Command;

public sealed class MfaEmailV1SendCommand : IHandleableCommand<
    MfaEmailV1SendCommand,
    MfaEmailV1SendCommandValidator,
    MfaEmailV1SendCommandHandler,
    MfaEmailV1SendCommandResult>
{
    public required string PendingId { get; init; }
}
