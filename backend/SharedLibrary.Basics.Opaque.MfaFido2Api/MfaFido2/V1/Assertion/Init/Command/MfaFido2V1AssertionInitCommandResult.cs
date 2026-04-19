namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Command;

public sealed class MfaFido2V1AssertionInitCommandResult : IMfaFido2V1AssertionInitCommandResultQueue
{
    public required string OptionsJson { get; init; }
}
