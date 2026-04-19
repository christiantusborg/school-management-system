using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Command;

public sealed class RecoveryCodesV1InitCommandResult : IRecoveryCodesV1InitCommandResultQueue
{
    public required Guid BatchId { get; init; }
    public required string[] EvaluatedElements { get; init; }
}
