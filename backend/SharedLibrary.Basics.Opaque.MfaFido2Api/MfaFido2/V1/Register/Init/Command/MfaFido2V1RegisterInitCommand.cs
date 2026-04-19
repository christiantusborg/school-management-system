namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Command;

public sealed class MfaFido2V1RegisterInitCommand : IHandleableCommand<
    MfaFido2V1RegisterInitCommand,
    MfaFido2V1RegisterInitCommandValidator,
    MfaFido2V1RegisterInitCommandHandler,
    MfaFido2V1RegisterInitCommandResult>
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
}
