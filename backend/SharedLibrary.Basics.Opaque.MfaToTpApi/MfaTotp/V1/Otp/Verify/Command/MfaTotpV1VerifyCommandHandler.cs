using OtpNet;
using Odin.Api.Base.Authentication;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaTotpV1VerifyCommandHandler(
    ITransientStateCache transientStateCache,
    IUserTwoFactorMethodRepository repository,
    SessionTokenService sessionTokenService)
    : ICommandHandler<MfaTotpV1VerifyCommand, MfaTotpV1VerifyCommandResult,
        SessionToken, ISessionTokenRepository>
{
    public async Task<SuccessOrFailure<MfaTotpV1VerifyCommandResult>> HandleAsync(
        MfaTotpV1VerifyCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<MfaPendingState>($"mfa:{command.PendingId}");
        if (state is null)
            return SuccessOrFailureHelper<MfaTotpV1VerifyCommandResult>.Create(
                $"{nameof(MfaTotpV1VerifyCommand)} - Invalid or expired session.");

        var spec = new Specification<UserTwoFactorMethod>()
            .AddWhere(x => x.UserId == state.UserId)
            .AddWhere(x => x.MethodType == MfaMethodType.Totp);

        var method = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (method?.TotpSecret is null)
            return SuccessOrFailureHelper<MfaTotpV1VerifyCommandResult>.Create(
                $"{nameof(MfaTotpV1VerifyCommand)} - TOTP not configured.");

        var totp = new Totp(Base32Encoding.ToBytes(method.TotpSecret));
        var valid = totp.VerifyTotp(DateTime.UtcNow, command.Code, out long step,
            VerificationWindow.RfcSpecifiedNetworkDelay);

        if (!valid || method.LastTotpStepUsed == step)
            return SuccessOrFailureHelper<MfaTotpV1VerifyCommandResult>.Create(
                $"{nameof(MfaTotpV1VerifyCommand)} - Invalid code.");

        method.LastTotpStepUsed = step;

        await transientStateCache.RemoveAsync($"mfa:{command.PendingId}");

        var (rawToken, session) = await sessionTokenService.CreateTokenAsync(
            state.UserId, state.DeviceInfo, cancellationToken);

        return new SuccessOrFailure<MfaTotpV1VerifyCommandResult>(
            new MfaTotpV1VerifyCommandResult { Token = rawToken, ExpiresAt = session.ExpiresAt });
    }
}
