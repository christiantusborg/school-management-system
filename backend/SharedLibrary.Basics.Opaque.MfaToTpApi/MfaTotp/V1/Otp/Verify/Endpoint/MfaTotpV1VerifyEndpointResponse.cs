namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Endpoint;

public class MfaTotpV1VerifyEndpointResponse : HateoasBaseResponse
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
