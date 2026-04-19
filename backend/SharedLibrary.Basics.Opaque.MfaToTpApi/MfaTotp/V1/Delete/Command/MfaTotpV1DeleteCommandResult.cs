using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Command;

public sealed class MfaTotpV1DeleteCommandResult : IMfaTotpV1DeleteCommandResultQueue
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
