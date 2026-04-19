namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Endpoint;

public class MfaEmailV1VerifyEndpointResponse : HateoasBaseResponse
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
