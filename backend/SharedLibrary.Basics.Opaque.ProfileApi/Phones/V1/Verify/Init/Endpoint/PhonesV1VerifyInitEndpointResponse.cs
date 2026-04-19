namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Endpoint;

public class PhonesV1VerifyInitEndpointResponse : HateoasBaseResponse
{
    public required Guid SessionId { get; init; }
}
