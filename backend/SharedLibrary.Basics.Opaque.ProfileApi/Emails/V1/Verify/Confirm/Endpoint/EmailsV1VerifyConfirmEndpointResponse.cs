namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Endpoint;

public class EmailsV1VerifyConfirmEndpointResponse : HateoasBaseResponse
{
    public required Guid UserContactEmailId { get; init; }
}
