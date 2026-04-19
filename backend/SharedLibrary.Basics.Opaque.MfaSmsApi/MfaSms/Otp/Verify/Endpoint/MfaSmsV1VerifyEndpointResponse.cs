namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Endpoint;

public class MfaSmsV1VerifyEndpointResponse : HateoasBaseResponse
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
