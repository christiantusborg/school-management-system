namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Endpoint;

public class EmailsV1CreateEndpointResponse : HateoasBaseResponse
{
    public required Guid UserContactEmailId { get; init; }
}
