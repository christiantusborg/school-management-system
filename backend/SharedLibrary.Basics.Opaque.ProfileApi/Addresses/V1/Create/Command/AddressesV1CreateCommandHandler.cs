namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AddressesV1CreateCommandHandler(IUserAddressRepository repository)
    : ICommandHandler<AddressesV1CreateCommand, AddressesV1CreateCommandResult, UserAddress, IUserAddressRepository>
{
    public async Task<SuccessOrFailure<AddressesV1CreateCommandResult>> HandleAsync(
        AddressesV1CreateCommand command, CancellationToken cancellationToken)
    {
        var countSpec = new Specification<UserAddress>()
            .AddWhere(x => x.UserId == command.UserId);
        var count = await repository.CountAsync(countSpec, cancellationToken).ConfigureAwait(false);
        if (count >= 10)
            return SuccessOrFailureHelper<AddressesV1CreateCommandResult>.Create(
                $"{nameof(AddressesV1CreateCommand)} - Maximum of 10 addresses allowed.");

        var entity = new UserAddress
        {
            UserId = command.UserId,
            Label = command.Label,
            Street = command.Street,
            City = command.City,
            State = command.State,
            ZipCode = command.ZipCode,
            Country = command.Country,
            IsPrimary = count == 0
        };
        repository.Add(entity);


        return new SuccessOrFailure<AddressesV1CreateCommandResult>(
            new AddressesV1CreateCommandResult { UserAddressId = entity.UserAddressId });
    }
}
