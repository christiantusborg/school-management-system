namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Command;

public sealed class MfaFido2V1RegisterInitCommandResult : IMfaFido2V1RegisterInitCommandResultQueue
{
    public required string OptionsJson { get; init; }
}
