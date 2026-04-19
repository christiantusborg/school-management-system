namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Command;

public sealed record AddressesV1UpdateCommand : IHandleableCommand<
    AddressesV1UpdateCommand,
    AddressesV1UpdateCommandValidator,
    AddressesV1UpdateCommandHandler,
    AddressesV1UpdateCommandResult>
{
    public required Guid UserAddressId { get; init; }
    public required string UserId { get; init; }
    public string? Label { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
}
