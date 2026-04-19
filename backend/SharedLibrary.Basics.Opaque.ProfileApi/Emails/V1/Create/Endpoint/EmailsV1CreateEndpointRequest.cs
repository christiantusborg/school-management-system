namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Endpoint;

public class EmailsV1CreateEndpointRequest
{
    public required string Email { get; init; }
    public string? Label { get; init; }
}
