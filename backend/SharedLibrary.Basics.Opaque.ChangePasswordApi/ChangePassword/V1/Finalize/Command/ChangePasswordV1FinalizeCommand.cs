namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Command;

public sealed class ChangePasswordV1FinalizeCommand : IHandleableCommand<
    ChangePasswordV1FinalizeCommand,
    ChangePasswordV1FinalizeCommandValidator,
    ChangePasswordV1FinalizeCommandHandler,
    ChangePasswordV1FinalizeCommandResult>
{
    public required Guid ChangeId { get; init; }
    public required string Signature { get; init; }
    public required string ClientPublicKey { get; init; }
}
