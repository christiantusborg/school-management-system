using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1DeleteCommandHandler(IUserContactEmailRepository repository)
    : ICommandHandler<EmailsV1DeleteCommand, EmailsV1DeleteCommandResult, UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<EmailsV1DeleteCommandResult>> HandleAsync(
        EmailsV1DeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserContactEmailId == command.UserContactEmailId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<EmailsV1DeleteCommandResult>.EntityNotFound(typeof(EmailsV1DeleteCommand));

        repository.Remove(existing);


        return new SuccessOrFailure<EmailsV1DeleteCommandResult>(
            new EmailsV1DeleteCommandResult { UserContactEmailId = existing.UserContactEmailId });
    }
}
