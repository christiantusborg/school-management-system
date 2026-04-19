using OtpNet;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaTotpV1EnableInitCommandHandler(
    ITransientStateCache transientStateCache)
    : ICommandHandler<MfaTotpV1EnableInitCommand, MfaTotpV1EnableInitCommandResult,
        UserTwoFactorMethod, IUserTwoFactorMethodRepository>
{
    public async Task<SuccessOrFailure<MfaTotpV1EnableInitCommandResult>> HandleAsync(
        MfaTotpV1EnableInitCommand command, CancellationToken cancellationToken)
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(20);
        var secret = Base32Encoding.ToString(secretBytes);
        var qrUri = $"otpauth://totp/Odin:{Uri.EscapeDataString(command.Username)}?secret={secret}&issuer=Odin&digits=6&period=30";

        await transientStateCache.SetAsync(
            $"totp-init:{command.UserId}",
            new MfaTotpEnableInitState { Secret = secret },
            TimeSpan.FromMinutes(5));

        return new SuccessOrFailure<MfaTotpV1EnableInitCommandResult>(
            new MfaTotpV1EnableInitCommandResult { Secret = secret, QrUri = qrUri });
    }
}
