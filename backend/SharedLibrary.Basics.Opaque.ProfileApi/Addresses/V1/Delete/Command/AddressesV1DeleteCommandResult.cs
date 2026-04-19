namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Command;

public sealed class AddressesV1DeleteCommandResult : IAddressesV1DeleteCommandResultQueue
{
    public required Guid UserAddressId { get; init; }
}
