using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1DeleteCommandHandler(IUserAddressRepository repository)
    : ICommandHandler<AddressesV1DeleteCommand, AddressesV1DeleteCommandResult, UserAddress, IUserAddressRepository>
{
    public async Task<SuccessOrFailure<AddressesV1DeleteCommandResult>> HandleAsync(
        AddressesV1DeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserAddress>()
            .AddWhere(x => x.UserAddressId == command.UserAddressId)
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<AddressesV1DeleteCommandResult>.EntityNotFound(typeof(AddressesV1DeleteCommand));

        repository.Remove(existing);


        return new SuccessOrFailure<AddressesV1DeleteCommandResult>(
            new AddressesV1DeleteCommandResult { UserAddressId = existing.UserAddressId });
    }
}
