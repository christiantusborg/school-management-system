namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Endpoint;

public class EmailsV1VerifyConfirmEndpointRequest
{
    public required Guid SessionId { get; init; }
    public required string Code { get; init; }
}
