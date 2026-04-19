using System.Text.Json;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Command;

public sealed class MfaFido2V1AssertionFinishCommand : IHandleableCommand<
    MfaFido2V1AssertionFinishCommand,
    MfaFido2V1AssertionFinishCommandValidator,
    MfaFido2V1AssertionFinishCommandHandler,
    MfaFido2V1AssertionFinishCommandResult>
{
    public required string PendingId { get; init; }
    public required JsonElement AssertionResponse { get; init; }
}
