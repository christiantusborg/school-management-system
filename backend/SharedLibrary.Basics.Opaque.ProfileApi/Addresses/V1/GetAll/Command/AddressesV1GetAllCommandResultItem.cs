using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Command;

public sealed class AddressesV1GetAllCommandResultItem : IAddressesV1GetAllCommandResultItemQueue
{
    public required Guid UserAddressId { get; init; }
    public string? Label { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public required bool IsPrimary { get; init; }
}
