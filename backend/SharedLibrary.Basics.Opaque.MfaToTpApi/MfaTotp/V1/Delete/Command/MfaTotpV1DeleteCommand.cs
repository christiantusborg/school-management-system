namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Command;

public sealed class MfaTotpV1DeleteCommand : IHandleableCommand<
    MfaTotpV1DeleteCommand,
    MfaTotpV1DeleteCommandValidator,
    MfaTotpV1DeleteCommandHandler,
    MfaTotpV1DeleteCommandResult>
{
    public required string UserId { get; init; }
}
