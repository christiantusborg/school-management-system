namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Endpoint;

public class MfaEmailV1EnableInitEndpointResponse : HateoasBaseResponse
{
    public required Guid SessionId { get; init; }
}
