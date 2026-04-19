namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Endpoint;

public class AddressesV1SetPrimaryEndpointResponse : HateoasBaseResponse
{
    public required Guid UserAddressId { get; init; }
}
