namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;

public sealed class RegisterFinalizeV1CreateCommand : IHandleableCommand<
    RegisterFinalizeV1CreateCommand,
    RegisterFinalizeV1CreateCommandValidator,
    RegisterFinalizeV1CreateCommandHandler,
    RegisterFinalizeV1CreateCommandResult>
{
    public required Guid RegistrationId { get; init; }
    public required string ClientPublicKey { get; init; }
}
