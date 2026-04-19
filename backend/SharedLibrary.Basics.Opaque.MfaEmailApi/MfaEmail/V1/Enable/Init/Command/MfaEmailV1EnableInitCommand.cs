namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Command;

public sealed class MfaEmailV1EnableInitCommand : IHandleableCommand<
    MfaEmailV1EnableInitCommand,
    MfaEmailV1EnableInitCommandValidator,
    MfaEmailV1EnableInitCommandHandler,
    MfaEmailV1EnableInitCommandResult>
{
    public required string UserId { get; init; }
}
