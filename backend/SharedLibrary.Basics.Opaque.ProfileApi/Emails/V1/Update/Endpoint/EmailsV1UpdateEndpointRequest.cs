namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Endpoint;

public class EmailsV1UpdateEndpointRequest
{
    public required string Email { get; init; }
    public string? Label { get; init; }
}
