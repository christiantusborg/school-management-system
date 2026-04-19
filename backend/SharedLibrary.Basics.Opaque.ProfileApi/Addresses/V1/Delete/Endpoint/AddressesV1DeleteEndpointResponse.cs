namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Endpoint;

public class AddressesV1DeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid UserAddressId { get; init; }
}
