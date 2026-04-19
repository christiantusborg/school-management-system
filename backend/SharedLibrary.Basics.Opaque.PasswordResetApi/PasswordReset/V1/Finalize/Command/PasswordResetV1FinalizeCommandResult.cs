namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Command;

public sealed class PasswordResetV1FinalizeCommandResult : IPasswordResetV1FinalizeCommandResultQueue
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
