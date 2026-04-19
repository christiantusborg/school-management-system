using SharedLibrary.Basics.Opaque.Domains.Repositories;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify;
using SharedLibrary.Basics.TransientStateCache;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1VerifyConfirmCommandHandler(
    IUserContactEmailRepository repository,
    ITransientStateCache transientStateCache)
    : ICommandHandler<EmailsV1VerifyConfirmCommand, EmailsV1VerifyConfirmCommandResult,
        UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<EmailsV1VerifyConfirmCommandResult>> HandleAsync(
        EmailsV1VerifyConfirmCommand command, CancellationToken cancellationToken)
    {
        var state = await transientStateCache.GetAsync<EmailVerifyState>($"email-verify:{command.SessionId}");
        if (state is null)
            return SuccessOrFailureHelper<EmailsV1VerifyConfirmCommandResult>.EntityNotFound(
                typeof(EmailsV1VerifyConfirmCommand));

        if (!string.Equals(state.Code, command.Code, StringComparison.Ordinal))
            return SuccessOrFailureHelper<EmailsV1VerifyConfirmCommandResult>.Create(
                "Invalid verification code.");

        await transientStateCache.RemoveAsync($"email-verify:{command.SessionId}");

        var spec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserContactEmailId == state.UserContactEmailId)
            .AddWhere(x => x.UserId == state.UserId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return SuccessOrFailureHelper<EmailsV1VerifyConfirmCommandResult>.EntityNotFound(
                typeof(EmailsV1VerifyConfirmCommand));

        entity.IsVerified = true;

        return new SuccessOrFailure<EmailsV1VerifyConfirmCommandResult>(
            new EmailsV1VerifyConfirmCommandResult { UserContactEmailId = entity.UserContactEmailId });
    }
}
