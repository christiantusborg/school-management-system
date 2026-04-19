namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Endpoint;

public class EmailsV1SetPrimaryEndpointResponse : HateoasBaseResponse
{
    public required Guid UserContactEmailId { get; init; }
}
