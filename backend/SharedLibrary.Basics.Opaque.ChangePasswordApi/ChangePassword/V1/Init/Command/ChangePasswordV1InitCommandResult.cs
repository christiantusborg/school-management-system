using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Command;

public sealed class ChangePasswordV1InitCommandResult : IChangePasswordV1InitCommandResultQueue
{
    public required Guid ChangeId { get; init; }
    public required string OldEvaluatedElement { get; init; }
    public required string Challenge { get; init; }
    public required string EvaluatedElement { get; init; }
}
