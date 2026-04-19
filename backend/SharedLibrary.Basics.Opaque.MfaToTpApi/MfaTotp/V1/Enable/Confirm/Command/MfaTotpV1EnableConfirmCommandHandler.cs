using OtpNet;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaTotpV1EnableConfirmCommandHandler(
    ITransientStateCache transientStateCache,
    IUserTwoFactorMethodRepository repository)
    : ICommandHandler<MfaTotpV1EnableConfirmCommand, MfaTotpV1EnableConfirmCommandResult,
        UserTwoFactorMethod, IUserTwoFactorMethodRepository>
{
    public async Task<SuccessOrFailure<MfaTotpV1EnableConfirmCommandResult>> HandleAsync(
        MfaTotpV1EnableConfirmCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"totp-init:{command.UserId}";
        var state = await transientStateCache.GetAsync<MfaTotpEnableInitState>(cacheKey);
        if (state is null)
            return SuccessOrFailureHelper<MfaTotpV1EnableConfirmCommandResult>.Create(
                $"{nameof(MfaTotpV1EnableConfirmCommand)} - TOTP setup not initiated or expired.");

        var totp = new Totp(Base32Encoding.ToBytes(state.Secret));
        var valid = totp.VerifyTotp(DateTime.UtcNow, command.Code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        if (!valid)
            return SuccessOrFailureHelper<MfaTotpV1EnableConfirmCommandResult>.Create(
                $"{nameof(MfaTotpV1EnableConfirmCommand)} - Invalid code.");

        await transientStateCache.RemoveAsync(cacheKey);

        var spec = new Specification<UserTwoFactorMethod>()
            .AddWhere(x => x.UserId == command.UserId)
            .AddWhere(x => x.MethodType == MfaMethodType.Totp);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        UserTwoFactorMethod method;
        if (existing is not null)
        {
            existing.TotpSecret = state.Secret;
            method = existing;
        }
        else
        {
            method = new UserTwoFactorMethod
            {
                UserId = command.UserId,
                MethodType = MfaMethodType.Totp,
                TotpSecret = state.Secret
            };
            repository.Add(method);
        }


        return new SuccessOrFailure<MfaTotpV1EnableConfirmCommandResult>(
            new MfaTotpV1EnableConfirmCommandResult { UserTwoFactorMethodId = method.UserTwoFactorMethodId });
    }
}
