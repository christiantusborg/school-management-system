using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1GetCommandHandler(IUserAddressRepository repository)
    : ICommandHandler<AddressesV1GetCommand, AddressesV1GetCommandResult, UserAddress, IUserAddressRepository>
{
    public async Task<SuccessOrFailure<AddressesV1GetCommandResult>> HandleAsync(
        AddressesV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserAddress>()
            .AddWhere(x => x.UserAddressId == command.UserAddressId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<AddressesV1GetCommandResult>.EntityNotFound(typeof(AddressesV1GetCommand));

        return new SuccessOrFailure<AddressesV1GetCommandResult>(new AddressesV1GetCommandResult
        {
            UserAddressId = existing.UserAddressId,
            Label = existing.Label,
            Street = existing.Street,
            City = existing.City,
            State = existing.State,
            ZipCode = existing.ZipCode,
            Country = existing.Country,
            IsPrimary = existing.IsPrimary
        });
    }
}
