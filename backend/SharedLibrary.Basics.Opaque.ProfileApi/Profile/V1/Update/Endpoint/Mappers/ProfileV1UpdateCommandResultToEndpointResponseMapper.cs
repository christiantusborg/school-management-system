using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Endpoint.Mappers;

public class ProfileV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ProfileV1UpdateCommandResult, ProfileV1UpdateEndpointResponse>
{
    public ProfileV1UpdateEndpointResponse MapFrom(ProfileV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProfileV1UpdateEndpointResponse
        {
            UserProfileId = input.UserProfileId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserProfileId)
        };
    }
}
