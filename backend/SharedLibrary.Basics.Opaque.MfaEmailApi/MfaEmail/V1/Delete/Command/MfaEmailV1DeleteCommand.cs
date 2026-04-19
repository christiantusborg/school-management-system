namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Command;

public sealed class MfaEmailV1DeleteCommand : IHandleableCommand<
    MfaEmailV1DeleteCommand,
    MfaEmailV1DeleteCommandValidator,
    MfaEmailV1DeleteCommandHandler,
    MfaEmailV1DeleteCommandResult>
{
    public required string UserId { get; init; }
}
