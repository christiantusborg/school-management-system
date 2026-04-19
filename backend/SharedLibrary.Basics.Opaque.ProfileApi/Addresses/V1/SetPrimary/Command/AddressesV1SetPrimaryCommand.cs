namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Command;

public sealed class AddressesV1SetPrimaryCommand : IHandleableCommand<
    AddressesV1SetPrimaryCommand,
    AddressesV1SetPrimaryCommandValidator,
    AddressesV1SetPrimaryCommandHandler,
    AddressesV1SetPrimaryCommandResult>
{
    public required Guid UserAddressId { get; init; }
    public required string UserId { get; init; }
}
