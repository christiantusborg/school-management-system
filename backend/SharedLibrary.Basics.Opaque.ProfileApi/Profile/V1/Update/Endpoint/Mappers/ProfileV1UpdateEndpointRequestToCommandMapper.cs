using SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Endpoint.Mappers;

public class ProfileV1UpdateEndpointRequestToCommandMapper
    : IMapper<ProfileV1UpdateEndpointRequest, ProfileV1UpdateCommand>
{
    public ProfileV1UpdateCommand MapFrom(ProfileV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new ProfileV1UpdateCommand
        {
            UserId = string.Empty, // overwritten in endpoint
            FirstName = input.FirstName,
            LastName = input.LastName,
            AvatarUrl = input.AvatarUrl,
            Bio = input.Bio,
            DateOfBirth = input.DateOfBirth,
        };
    }
}
