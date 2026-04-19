namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Command;

public sealed class PasswordResetV1FinalizeCommand
    : IHandleableCommand<PasswordResetV1FinalizeCommand, PasswordResetV1FinalizeCommandValidator,
        PasswordResetV1FinalizeCommandHandler, PasswordResetV1FinalizeCommandResult>
{
    public required string ResetId { get; init; }
    public required string ClientPublicKey { get; init; }
}
