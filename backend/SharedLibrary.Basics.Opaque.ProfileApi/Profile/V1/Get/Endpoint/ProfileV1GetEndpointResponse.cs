namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Endpoint;

public class ProfileV1GetEndpointResponse : HateoasBaseResponse
{
    public required Guid UserProfileId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public DateTime? DateOfBirth { get; init; }
}
