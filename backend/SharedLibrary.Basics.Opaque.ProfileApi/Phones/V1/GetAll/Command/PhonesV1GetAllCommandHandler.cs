using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1GetAllCommandHandler(IUserPhoneRepository repository)
    : ICommandHandler<PhonesV1GetAllCommand, CommandSearchResult<PhonesV1GetAllCommandResultItem>, UserPhone, IUserPhoneRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<PhonesV1GetAllCommandResultItem>>> HandleAsync(
        PhonesV1GetAllCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserPhone>()
            .AddWhere(x => x.UserId == command.UserId);

        var items = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);
        var count = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        var resultItems = items.Select(x => new PhonesV1GetAllCommandResultItem
        {
            UserPhoneId = x.UserPhoneId,
            Number = x.Number,
            Label = x.Label,
            IsPrimary = x.IsPrimary,
            IsVerified = x.IsVerified
        }).ToList();

        var result = new CommandSearchResult<PhonesV1GetAllCommandResultItem>
        {
            Items = resultItems,
            Total = count
        };

        return new SuccessOrFailure<CommandSearchResult<PhonesV1GetAllCommandResultItem>>(result);
    }
}
