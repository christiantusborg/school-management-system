namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Command;

public sealed class MfaTotpV1EnableConfirmCommand : IHandleableCommand<
    MfaTotpV1EnableConfirmCommand,
    MfaTotpV1EnableConfirmCommandValidator,
    MfaTotpV1EnableConfirmCommandHandler,
    MfaTotpV1EnableConfirmCommandResult>
{
    public required string UserId { get; init; }
    public required string Code { get; init; }
}
