namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Endpoint;

public class EmailsV1VerifyInitEndpointResponse : HateoasBaseResponse
{
    public required Guid SessionId { get; init; }
}
