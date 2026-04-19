using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Command;

public sealed class MfaEmailV1EnableInitCommandResult : IMfaEmailV1EnableInitCommandResultQueue
{
    public required Guid SessionId { get; init; }
}
