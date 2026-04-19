namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Endpoint;

public class MfaTotpV1EnableInitEndpointResponse : HateoasBaseResponse
{
    public required string Secret { get; init; }
    public required string QrUri { get; init; }
}
