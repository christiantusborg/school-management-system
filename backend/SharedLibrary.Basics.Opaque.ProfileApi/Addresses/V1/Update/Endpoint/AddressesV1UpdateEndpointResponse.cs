namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Endpoint;

public class AddressesV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid UserAddressId { get; init; }
}
