namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Endpoint;

public class MfaSmsV1EnableInitEndpointResponse : HateoasBaseResponse
{
    public required Guid SessionId { get; init; }
}
