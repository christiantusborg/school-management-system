using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Endpoint.Mappers;

public class ProfileV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<ProfileV1GetCommandResult, ProfileV1GetEndpointResponse>
{
    public ProfileV1GetEndpointResponse MapFrom(ProfileV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProfileV1GetEndpointResponse
        {
            UserProfileId = input.UserProfileId,
            FirstName = input.FirstName,
            LastName = input.LastName,
            AvatarUrl = input.AvatarUrl,
            Bio = input.Bio,
            DateOfBirth = input.DateOfBirth,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserProfileId)
        };
    }
}
