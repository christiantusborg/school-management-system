namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Command;

public sealed class MfaTotpV1EnableInitCommand : IHandleableCommand<
    MfaTotpV1EnableInitCommand,
    MfaTotpV1EnableInitCommandValidator,
    MfaTotpV1EnableInitCommandHandler,
    MfaTotpV1EnableInitCommandResult>
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
}
