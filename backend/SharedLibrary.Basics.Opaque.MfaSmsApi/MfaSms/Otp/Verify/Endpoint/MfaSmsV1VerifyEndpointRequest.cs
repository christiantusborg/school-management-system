namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Endpoint;

public class MfaSmsV1VerifyEndpointRequest
{
    public required string PendingId { get; init; }
    public required string Code { get; init; }
}
