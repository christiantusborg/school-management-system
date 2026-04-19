using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Command;

public sealed class ChangePasswordV1FinalizeCommandResult : IChangePasswordV1FinalizeCommandResultQueue
{
    public required Guid ChangeId { get; init; }
}
