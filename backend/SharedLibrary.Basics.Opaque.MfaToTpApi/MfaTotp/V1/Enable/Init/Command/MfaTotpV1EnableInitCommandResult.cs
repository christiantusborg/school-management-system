using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Command;

public sealed class MfaTotpV1EnableInitCommandResult : IMfaTotpV1EnableInitCommandResultQueue
{
    public required string Secret { get; init; }
    public required string QrUri { get; init; }
}
