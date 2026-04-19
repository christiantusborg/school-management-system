using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Command;

public sealed class MfaEmailV1EnableConfirmCommandResult : IMfaEmailV1EnableConfirmCommandResultQueue
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
