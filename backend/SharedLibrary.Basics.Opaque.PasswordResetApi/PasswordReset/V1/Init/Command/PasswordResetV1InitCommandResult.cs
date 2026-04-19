namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Command;

public sealed class PasswordResetV1InitCommandResult : IPasswordResetV1InitCommandResultQueue
{
    public required string ResetId { get; init; }
    public required string EvaluatedElement { get; init; }
}
