using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1GetCommandHandler(IUserContactEmailRepository repository)
    : ICommandHandler<EmailsV1GetCommand, EmailsV1GetCommandResult, UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<EmailsV1GetCommandResult>> HandleAsync(
        EmailsV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserContactEmailId == command.UserContactEmailId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<EmailsV1GetCommandResult>.EntityNotFound(typeof(EmailsV1GetCommand));

        return new SuccessOrFailure<EmailsV1GetCommandResult>(new EmailsV1GetCommandResult
        {
            UserContactEmailId = existing.UserContactEmailId,
            Email = existing.Email,
            Label = existing.Label,
            IsPrimary = existing.IsPrimary
        });
    }
}
