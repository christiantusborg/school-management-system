using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Command;

public sealed class MfaEmailV1DeleteCommandResult : IMfaEmailV1DeleteCommandResultQueue
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
