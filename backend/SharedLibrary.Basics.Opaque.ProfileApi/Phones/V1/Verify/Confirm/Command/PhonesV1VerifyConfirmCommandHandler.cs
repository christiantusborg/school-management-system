using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1VerifyConfirmCommandHandler(
    IUserPhoneRepository repository,
    ITransientStateCache transientStateCache)
    : ICommandHandler<PhonesV1VerifyConfirmCommand, PhonesV1VerifyConfirmCommandResult,
        UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<PhonesV1VerifyConfirmCommandResult>> HandleAsync(
        PhonesV1VerifyConfirmCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<PhoneVerifyState>($"phone-verify:{command.SessionId}");
        if (state is null)
            return SuccessOrFailureHelper<PhonesV1VerifyConfirmCommandResult>.EntityNotFound(
                typeof(PhonesV1VerifyConfirmCommand));

        if (!string.Equals(state.Code, command.Code, StringComparison.Ordinal))
            return SuccessOrFailureHelper<PhonesV1VerifyConfirmCommandResult>.Create(
                "Invalid verification code.");

        await transientStateCache.RemoveAsync($"phone-verify:{command.SessionId}");

        var spec = new Specification<UserPhone>()
            .AddWhere(x => x.UserPhoneId == state.UserPhoneId)
            .AddWhere(x => x.UserId == state.UserId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return SuccessOrFailureHelper<PhonesV1VerifyConfirmCommandResult>.EntityNotFound(
                typeof(PhonesV1VerifyConfirmCommand));

        entity.IsVerified = true;

        return new SuccessOrFailure<PhonesV1VerifyConfirmCommandResult>(
            new PhonesV1VerifyConfirmCommandResult { UserPhoneId = entity.UserPhoneId });
    }
}
