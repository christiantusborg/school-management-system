using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1CreateCommandHandler(IUserContactEmailRepository repository)
    : ICommandHandler<EmailsV1CreateCommand, EmailsV1CreateCommandResult, UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<EmailsV1CreateCommandResult>> HandleAsync(
        EmailsV1CreateCommand command, CancellationToken cancellationToken)
    {
        var countSpec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserId == command.UserId);
        var count = await repository.CountAsync(countSpec, cancellationToken).ConfigureAwait(false);
        if (count >= 10)
            return SuccessOrFailureHelper<EmailsV1CreateCommandResult>.Create(
                $"{nameof(EmailsV1CreateCommand)} - Maximum of 10 contact emails allowed.");

        var entity = new UserContactEmail
        {
            UserId = command.UserId,
            Email = command.Email,
            Label = command.Label,
            IsPrimary = count == 0
        };
        repository.Add(entity);


        return new SuccessOrFailure<EmailsV1CreateCommandResult>(
            new EmailsV1CreateCommandResult { UserContactEmailId = entity.UserContactEmailId });
    }
}
