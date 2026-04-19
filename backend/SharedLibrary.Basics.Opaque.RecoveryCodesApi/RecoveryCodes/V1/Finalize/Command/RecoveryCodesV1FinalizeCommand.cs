namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Command;

public sealed class RecoveryCodesV1FinalizeCommand : IHandleableCommand<
    RecoveryCodesV1FinalizeCommand,
    RecoveryCodesV1FinalizeCommandValidator,
    RecoveryCodesV1FinalizeCommandHandler,
    RecoveryCodesV1FinalizeCommandResult>
{
    public required Guid BatchId { get; init; }
    public required string[] ClientPublicKeys { get; init; }
    public required string[] EncryptedPrivateKeys { get; init; }
    public required string[] Nonces { get; init; }
}
