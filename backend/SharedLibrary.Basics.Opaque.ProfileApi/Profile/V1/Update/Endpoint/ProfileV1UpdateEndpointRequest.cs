namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Endpoint;

public class ProfileV1UpdateEndpointRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public DateTime? DateOfBirth { get; init; }
}
