namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Endpoint;

public class PhonesV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid UserPhoneId { get; init; }
}
