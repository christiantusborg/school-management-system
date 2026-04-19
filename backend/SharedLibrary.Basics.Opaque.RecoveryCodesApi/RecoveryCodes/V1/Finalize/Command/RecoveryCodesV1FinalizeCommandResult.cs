using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Command;

public sealed class RecoveryCodesV1FinalizeCommandResult : IRecoveryCodesV1FinalizeCommandResultQueue
{
    public required Guid BatchId { get; init; }
}
