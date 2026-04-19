using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Command;

public sealed class MfaSmsV1EnableConfirmCommandResult : IMfaSmsV1EnableConfirmCommandResultQueue
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
