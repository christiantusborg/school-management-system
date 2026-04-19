using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1GetAllCommandHandler(IUserAddressRepository repository)
    : ICommandHandler<AddressesV1GetAllCommand, CommandSearchResult<AddressesV1GetAllCommandResultItem>, UserAddress, IUserAddressRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<AddressesV1GetAllCommandResultItem>>> HandleAsync(
        AddressesV1GetAllCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserAddress>()
            .AddWhere(x => x.UserId == command.UserId);

        var items = await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);
        var count = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        var resultItems = items.Select(x => new AddressesV1GetAllCommandResultItem
        {
            UserAddressId = x.UserAddressId,
            Label = x.Label,
            Street = x.Street,
            City = x.City,
            State = x.State,
            ZipCode = x.ZipCode,
            Country = x.Country,
            IsPrimary = x.IsPrimary
        }).ToList();

        var result = new CommandSearchResult<AddressesV1GetAllCommandResultItem>
        {
            Items = resultItems,
            Total = count
        };

        return new SuccessOrFailure<CommandSearchResult<AddressesV1GetAllCommandResultItem>>(result);
    }
}
