namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Command;

public sealed class MfaFido2V1DeleteCommandResult : IMfaFido2V1DeleteCommandResultQueue
{
    public required Guid Fido2CredentialId { get; init; }
}
