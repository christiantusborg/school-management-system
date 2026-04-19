using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1UpdateCommandHandler(IUserAddressRepository repository)
    : ICommandHandler<AddressesV1UpdateCommand, AddressesV1UpdateCommandResult, UserAddress, IUserAddressRepository>
{
    public async Task<SuccessOrFailure<AddressesV1UpdateCommandResult>> HandleAsync(
        AddressesV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserAddress>()
            .AddWhere(x => x.UserAddressId == command.UserAddressId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<AddressesV1UpdateCommandResult>.EntityNotFound(typeof(AddressesV1UpdateCommand));

        existing.Label = command.Label;
        existing.Street = command.Street;
        existing.City = command.City;
        existing.State = command.State;
        existing.ZipCode = command.ZipCode;
        existing.Country = command.Country;

        return new SuccessOrFailure<AddressesV1UpdateCommandResult>(
            new AddressesV1UpdateCommandResult { UserAddressId = existing.UserAddressId });
    }
}
