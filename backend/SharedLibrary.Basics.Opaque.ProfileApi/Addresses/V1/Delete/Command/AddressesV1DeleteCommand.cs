namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Command;

public sealed class AddressesV1DeleteCommand : IHandleableCommand<
    AddressesV1DeleteCommand,
    AddressesV1DeleteCommandValidator,
    AddressesV1DeleteCommandHandler,
    AddressesV1DeleteCommandResult>
{
    public required Guid UserAddressId { get; init; }
    public required string UserId { get; init; }
}
