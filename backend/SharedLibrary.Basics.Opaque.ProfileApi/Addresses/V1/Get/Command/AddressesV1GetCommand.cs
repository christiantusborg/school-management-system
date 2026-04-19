namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Get.Command;

public sealed class AddressesV1GetCommand : IHandleableCommand<
    AddressesV1GetCommand,
    AddressesV1GetCommandValidator,
    AddressesV1GetCommandHandler,
    AddressesV1GetCommandResult>
{
    public required Guid UserAddressId { get; init; }
    public required string UserId { get; init; }
}
