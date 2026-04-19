namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Endpoint;

public class AddressesV1CreateEndpointRequest
{
    public string? Label { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
}
