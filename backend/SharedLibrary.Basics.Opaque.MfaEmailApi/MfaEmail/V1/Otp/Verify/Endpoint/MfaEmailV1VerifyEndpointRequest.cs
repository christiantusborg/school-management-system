namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Endpoint;

public class MfaEmailV1VerifyEndpointRequest
{
    public required string PendingId { get; init; }
    public required string Code { get; init; }
}
