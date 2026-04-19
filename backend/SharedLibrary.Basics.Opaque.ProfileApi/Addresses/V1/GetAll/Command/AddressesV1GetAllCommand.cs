namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Command;

public sealed class AddressesV1GetAllCommand : IHandleableCommand<
    AddressesV1GetAllCommand,
    AddressesV1GetAllCommandValidator,
    AddressesV1GetAllCommandHandler,
    CommandSearchResult<AddressesV1GetAllCommandResultItem>>
{
    public required string UserId { get; init; }
}
