using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Command;

public sealed class MfaSmsV1DeleteCommandResult : IMfaSmsV1DeleteCommandResultQueue
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
