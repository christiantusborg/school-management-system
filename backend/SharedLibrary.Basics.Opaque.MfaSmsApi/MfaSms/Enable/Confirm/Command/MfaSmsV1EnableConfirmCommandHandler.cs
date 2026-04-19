using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaSmsV1EnableConfirmCommandHandler(
    ITransientStateCache transientStateCache,
    IUserTwoFactorMethodRepository repository)
    : ICommandHandler<MfaSmsV1EnableConfirmCommand, MfaSmsV1EnableConfirmCommandResult,
        UserTwoFactorMethod, IUserTwoFactorMethodRepository>
{
    public async Task<SuccessOrFailure<MfaSmsV1EnableConfirmCommandResult>> HandleAsync(
        MfaSmsV1EnableConfirmCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"mfa-sms-enable:{command.SessionId}";
        var state = await transientStateCache.GetAsync<MfaSmsEnableInitState>(cacheKey);
        if (state is null)
            return SuccessOrFailureHelper<MfaSmsV1EnableConfirmCommandResult>.EntityNotFound(
                typeof(MfaSmsV1EnableConfirmCommand));

        await transientStateCache.RemoveAsync(cacheKey);

        if (state.Code != command.Code)
            return SuccessOrFailureHelper<MfaSmsV1EnableConfirmCommandResult>.Create(
                $"{nameof(MfaSmsV1EnableConfirmCommand)} - Invalid verification code.");

        var method = new UserTwoFactorMethod
        {
            UserId = state.UserId,
            MethodType = MfaMethodType.Sms
        };

        repository.Add(method);


        return new SuccessOrFailure<MfaSmsV1EnableConfirmCommandResult>(
            new MfaSmsV1EnableConfirmCommandResult { UserTwoFactorMethodId = method.UserTwoFactorMethodId });
    }
}
