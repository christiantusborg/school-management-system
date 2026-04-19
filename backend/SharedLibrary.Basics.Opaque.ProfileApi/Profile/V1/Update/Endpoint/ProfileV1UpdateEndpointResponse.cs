namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Endpoint;

public class ProfileV1UpdateEndpointResponse : HateoasBaseResponse
{
    public required Guid UserProfileId { get; init; }
}
