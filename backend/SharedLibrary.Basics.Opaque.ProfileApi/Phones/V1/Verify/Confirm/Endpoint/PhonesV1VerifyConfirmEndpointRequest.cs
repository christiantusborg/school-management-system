namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Endpoint;

public class PhonesV1VerifyConfirmEndpointRequest
{
    public required Guid SessionId { get; init; }
    public required string Code { get; init; }
}
