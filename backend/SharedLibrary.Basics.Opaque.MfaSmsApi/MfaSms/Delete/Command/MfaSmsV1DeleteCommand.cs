namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Command;

public sealed class MfaSmsV1DeleteCommand : IHandleableCommand<
    MfaSmsV1DeleteCommand,
    MfaSmsV1DeleteCommandValidator,
    MfaSmsV1DeleteCommandHandler,
    MfaSmsV1DeleteCommandResult>
{
    public required string UserId { get; init; }
}
