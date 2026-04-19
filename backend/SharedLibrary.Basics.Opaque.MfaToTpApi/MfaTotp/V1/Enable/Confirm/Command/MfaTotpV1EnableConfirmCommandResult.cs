using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Command;

public sealed class MfaTotpV1EnableConfirmCommandResult : IMfaTotpV1EnableConfirmCommandResultQueue
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
