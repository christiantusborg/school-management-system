namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Command;

public sealed class AddressesV1SetPrimaryCommandResult : IAddressesV1SetPrimaryCommandResultQueue
{
    public required Guid UserAddressId { get; init; }
}
