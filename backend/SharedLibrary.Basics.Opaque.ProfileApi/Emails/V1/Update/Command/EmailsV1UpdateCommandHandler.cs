using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1UpdateCommandHandler(IUserContactEmailRepository repository)
    : ICommandHandler<EmailsV1UpdateCommand, EmailsV1UpdateCommandResult, UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<EmailsV1UpdateCommandResult>> HandleAsync(
        EmailsV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserContactEmailId == command.UserContactEmailId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<EmailsV1UpdateCommandResult>.EntityNotFound(typeof(EmailsV1UpdateCommand));

        existing.Email = command.Email;
        existing.Label = command.Label;

        return new SuccessOrFailure<EmailsV1UpdateCommandResult>(
            new EmailsV1UpdateCommandResult { UserContactEmailId = existing.UserContactEmailId });
    }
}
