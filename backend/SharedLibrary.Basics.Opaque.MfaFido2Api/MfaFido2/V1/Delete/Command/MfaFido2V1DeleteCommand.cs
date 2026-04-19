namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Command;

public sealed class MfaFido2V1DeleteCommand : IHandleableCommand<
    MfaFido2V1DeleteCommand,
    MfaFido2V1DeleteCommandValidator,
    MfaFido2V1DeleteCommandHandler,
    MfaFido2V1DeleteCommandResult>
{
    public required string UserId { get; init; }
    public required Guid Fido2CredentialId { get; init; }
}
