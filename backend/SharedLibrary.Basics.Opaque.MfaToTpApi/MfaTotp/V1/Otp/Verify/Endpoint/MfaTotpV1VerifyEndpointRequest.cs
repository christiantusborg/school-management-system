namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Endpoint;

public class MfaTotpV1VerifyEndpointRequest
{
    public required string PendingId { get; init; }
    public required string Code { get; init; }
}
