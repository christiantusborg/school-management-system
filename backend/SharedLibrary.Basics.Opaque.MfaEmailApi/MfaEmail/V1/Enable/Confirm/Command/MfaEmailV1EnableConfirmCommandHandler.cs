using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaEmailV1EnableConfirmCommandHandler(
    ITransientStateCache transientStateCache,
    IUserTwoFactorMethodRepository repository)
    : ICommandHandler<MfaEmailV1EnableConfirmCommand, MfaEmailV1EnableConfirmCommandResult,
        UserTwoFactorMethod, IUserTwoFactorMethodRepository>
{
    public async Task<SuccessOrFailure<MfaEmailV1EnableConfirmCommandResult>> HandleAsync(
        MfaEmailV1EnableConfirmCommand command, CancellationToken cancellationToken)
    {
        var cacheKey = $"mfa-email-enable:{command.SessionId}";
        var state = await transientStateCache.GetAsync<MfaEmailEnableInitState>(cacheKey);
        if (state is null)
            return SuccessOrFailureHelper<MfaEmailV1EnableConfirmCommandResult>.EntityNotFound(
                typeof(MfaEmailV1EnableConfirmCommand));

        await transientStateCache.RemoveAsync(cacheKey);

        if (state.Code != command.Code)
            return SuccessOrFailureHelper<MfaEmailV1EnableConfirmCommandResult>.Create(
                $"{nameof(MfaEmailV1EnableConfirmCommand)} - Invalid verification code.");

        var method = new UserTwoFactorMethod
        {
            UserId = state.UserId,
            MethodType = MfaMethodType.Email
        };

        repository.Add(method);


        return new SuccessOrFailure<MfaEmailV1EnableConfirmCommandResult>(
            new MfaEmailV1EnableConfirmCommandResult { UserTwoFactorMethodId = method.UserTwoFactorMethodId });
    }
}
