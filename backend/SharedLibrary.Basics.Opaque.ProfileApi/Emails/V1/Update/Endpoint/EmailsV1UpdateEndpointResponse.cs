namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Endpoint;

public class EmailsV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid UserContactEmailId { get; init; }
}
