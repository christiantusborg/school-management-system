namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Endpoint;

public class MfaEmailV1DeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
