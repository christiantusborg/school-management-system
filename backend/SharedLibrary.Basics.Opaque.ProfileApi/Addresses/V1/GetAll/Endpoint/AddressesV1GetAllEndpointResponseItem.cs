namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.GetAll.Endpoint;

public class AddressesV1GetAllEndpointResponseItem : HateoasBaseResponse
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
