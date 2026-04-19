using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EmailsV1GetAllCommandHandler(IUserContactEmailRepository repository)
    : ICommandHandler<EmailsV1GetAllCommand, CommandSearchResult<EmailsV1GetAllCommandResultItem>, UserContactEmail, IUserContactEmailRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<EmailsV1GetAllCommandResultItem>>> HandleAsync(
        EmailsV1GetAllCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserContactEmail>()
            .AddWhere(x => x.UserId == command.UserId);

        var items = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);
        var count = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        var resultItems = items.Select(x => new EmailsV1GetAllCommandResultItem
        {
            UserContactEmailId = x.UserContactEmailId,
            Email = x.Email,
            Label = x.Label,
            IsPrimary = x.IsPrimary,
            IsVerified = x.IsVerified
        }).ToList();

        var result = new CommandSearchResult<EmailsV1GetAllCommandResultItem>
        {
            Items = resultItems,
            Total = count
        };

        return new SuccessOrFailure<CommandSearchResult<EmailsV1GetAllCommandResultItem>>(result);
    }
}
