using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Command;

public sealed class MfaSmsV1EnableInitCommandResult : IMfaSmsV1EnableInitCommandResultQueue
{
    public required Guid SessionId { get; init; }
}
