using System.Text.Json;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Command;

public sealed record MfaFido2V1RegisterFinishCommand : IHandleableCommand<
    MfaFido2V1RegisterFinishCommand,
    MfaFido2V1RegisterFinishCommandValidator,
    MfaFido2V1RegisterFinishCommandHandler,
    MfaFido2V1RegisterFinishCommandResult>
{
    public required string UserId { get; init; }
    public required string Label { get; init; }
    public required JsonElement AttestationResponse { get; init; }
}
