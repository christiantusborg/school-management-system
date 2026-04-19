namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Command;

public sealed class AddressesV1UpdateCommandResult : IAddressesV1UpdateCommandResultQueue
{
    public required Guid UserAddressId { get; init; }
}
