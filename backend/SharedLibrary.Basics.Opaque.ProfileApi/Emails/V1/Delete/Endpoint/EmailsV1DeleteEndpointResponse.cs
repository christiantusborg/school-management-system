namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Endpoint;

public class EmailsV1DeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid UserContactEmailId { get; init; }
}
