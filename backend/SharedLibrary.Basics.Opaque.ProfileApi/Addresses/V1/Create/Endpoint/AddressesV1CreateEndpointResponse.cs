namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Endpoint;

public class AddressesV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid UserAddressId { get; init; }
}
