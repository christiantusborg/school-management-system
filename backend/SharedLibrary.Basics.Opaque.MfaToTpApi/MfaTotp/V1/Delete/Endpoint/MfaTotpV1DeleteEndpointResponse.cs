namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Endpoint;

public class MfaTotpV1DeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
