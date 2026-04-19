using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Command;

public sealed class RecoveryCodesV1GetStatusCommandResult : IRecoveryCodesV1GetStatusCommandResultQueue
{
    public required int RemainingCount { get; init; }
}
