namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Command;

public sealed record AddressesV1CreateCommand : IHandleableCommand<
    AddressesV1CreateCommand,
    AddressesV1CreateCommandValidator,
    AddressesV1CreateCommandHandler,
    AddressesV1CreateCommandResult>
{
    public required string UserId { get; init; }
    public string? Label { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
}
