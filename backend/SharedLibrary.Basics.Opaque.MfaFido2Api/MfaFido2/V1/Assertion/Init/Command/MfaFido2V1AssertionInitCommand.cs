namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Command;

public sealed class MfaFido2V1AssertionInitCommand : IHandleableCommand<
    MfaFido2V1AssertionInitCommand,
    MfaFido2V1AssertionInitCommandValidator,
    MfaFido2V1AssertionInitCommandHandler,
    MfaFido2V1AssertionInitCommandResult>
{
    public required string PendingId { get; init; }
}
