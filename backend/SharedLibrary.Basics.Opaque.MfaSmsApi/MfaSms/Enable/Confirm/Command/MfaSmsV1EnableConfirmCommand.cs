namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Command;

public sealed class MfaSmsV1EnableConfirmCommand : IHandleableCommand<
    MfaSmsV1EnableConfirmCommand,
    MfaSmsV1EnableConfirmCommandValidator,
    MfaSmsV1EnableConfirmCommandHandler,
    MfaSmsV1EnableConfirmCommandResult>
{
    public required Guid SessionId { get; init; }
    public required string Code { get; init; }
}
