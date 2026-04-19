namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Command;

public sealed class AddressesV1CreateCommandResult : IAddressesV1CreateCommandResultQueue
{
    public required Guid UserAddressId { get; init; }
}
