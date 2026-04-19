using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1SetPrimaryCommandHandler(IUserAddressRepository repository)
    : ICommandHandler<AddressesV1SetPrimaryCommand, AddressesV1SetPrimaryCommandResult, UserAddress, IUserAddressRepository>
{
    public async Task<SuccessOrFailure<AddressesV1SetPrimaryCommandResult>> HandleAsync(
        AddressesV1SetPrimaryCommand command, CancellationToken cancellationToken)
    {
        var targetSpec = new Specification<UserAddress>()
            .AddWhere(x => x.UserAddressId == command.UserAddressId)
            .AddWhere(x => x.UserId == command.UserId);

        var target = await repository.GetAsync(targetSpec, cancellationToken).ConfigureAwait(false);

        if (target == null)
            return SuccessOrFailureHelper<AddressesV1SetPrimaryCommandResult>.EntityNotFound(typeof(AddressesV1SetPrimaryCommand));

        var allSpec = new Specification<UserAddress>()
            .AddWhere(x => x.UserId == command.UserId);

        var all = await repository.SearchAsync(allSpec, cancellationToken).ConfigureAwait(false);

        foreach (var address in all)
            address.IsPrimary = false;

        target.IsPrimary = true;

        return new SuccessOrFailure<AddressesV1SetPrimaryCommandResult>(
            new AddressesV1SetPrimaryCommandResult { UserAddressId = target.UserAddressId });
    }
}
