namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Command;

public sealed class MfaEmailV1EnableConfirmCommand : IHandleableCommand<
    MfaEmailV1EnableConfirmCommand,
    MfaEmailV1EnableConfirmCommandValidator,
    MfaEmailV1EnableConfirmCommandHandler,
    MfaEmailV1EnableConfirmCommandResult>
{
    public required Guid SessionId { get; init; }
    public required string Code { get; init; }
}
