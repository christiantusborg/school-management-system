namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Command;

public sealed class MfaFido2V1AssertionFinishCommandResult : IMfaFido2V1AssertionFinishCommandResultQueue
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
