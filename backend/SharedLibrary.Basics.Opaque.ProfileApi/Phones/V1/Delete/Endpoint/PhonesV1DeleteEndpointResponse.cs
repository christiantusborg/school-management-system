namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Endpoint;

public class PhonesV1DeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid UserPhoneId { get; init; }
}
