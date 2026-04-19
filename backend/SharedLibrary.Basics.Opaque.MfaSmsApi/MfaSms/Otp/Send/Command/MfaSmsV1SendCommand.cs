namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Command;

public sealed class MfaSmsV1SendCommand : IHandleableCommand<
    MfaSmsV1SendCommand,
    MfaSmsV1SendCommandValidator,
    MfaSmsV1SendCommandHandler,
    MfaSmsV1SendCommandResult>
{
    public required string PendingId { get; init; }
}
