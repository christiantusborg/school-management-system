namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Command;

public sealed class MfaSmsV1EnableInitCommand : IHandleableCommand<
    MfaSmsV1EnableInitCommand,
    MfaSmsV1EnableInitCommandValidator,
    MfaSmsV1EnableInitCommandHandler,
    MfaSmsV1EnableInitCommandResult>
{
    public required string UserId { get; init; }
}
