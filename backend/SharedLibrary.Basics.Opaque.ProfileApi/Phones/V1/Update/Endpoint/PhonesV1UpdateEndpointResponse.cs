namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Endpoint;

public class PhonesV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid UserPhoneId { get; init; }
}
