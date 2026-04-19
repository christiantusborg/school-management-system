namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Endpoint;

public class MfaSmsV1DeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
