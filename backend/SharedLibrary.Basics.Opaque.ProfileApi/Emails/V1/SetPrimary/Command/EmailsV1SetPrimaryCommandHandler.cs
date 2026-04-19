using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1SetPrimaryCommandHandler(IUserContactEmailRepository repository)
    : ICommandHandler<EmailsV1SetPrimaryCommand, EmailsV1SetPrimaryCommandResult, UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<EmailsV1SetPrimaryCommandResult>> HandleAsync(
        EmailsV1SetPrimaryCommand command, CancellationToken cancellationToken)
    {
        var targetSpec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserContactEmailId == command.UserContactEmailId)
            .AddWhere(x => x.UserId == command.UserId);

        var target = await repository.GetAsync(targetSpec, cancellationToken).ConfigureAwait(false);

        if (target == null)
            return SuccessOrFailureHelper<EmailsV1SetPrimaryCommandResult>.EntityNotFound(typeof(EmailsV1SetPrimaryCommand));

        var allSpec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserId == command.UserId);

        var all = await repository.SearchAsync(allSpec, cancellationToken).ConfigureAwait(false);

        foreach (var email in all)
            email.IsPrimary = false;

        target.IsPrimary = true;

        return new SuccessOrFailure<EmailsV1SetPrimaryCommandResult>(
            new EmailsV1SetPrimaryCommandResult { UserContactEmailId = target.UserContactEmailId });
    }
}
