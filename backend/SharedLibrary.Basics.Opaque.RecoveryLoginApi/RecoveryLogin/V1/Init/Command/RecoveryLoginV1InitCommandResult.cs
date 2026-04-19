namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Command;

public sealed class RecoveryLoginV1InitCommandResult : IRecoveryLoginV1InitCommandResultQueue
{
    public required string LoginId { get; init; }
    public required string EvaluatedElement { get; init; }
    public required string Challenge { get; init; }
    public required string EncryptedPrivateKey { get; init; }
    public required string Nonce { get; init; }
}
