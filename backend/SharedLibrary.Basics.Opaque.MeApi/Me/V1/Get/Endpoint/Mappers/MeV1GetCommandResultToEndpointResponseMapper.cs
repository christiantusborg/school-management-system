using SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Endpoint.Mappers;

public sealed class MeV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MeV1GetCommandResult, MeV1GetEndpointResponse>
{
    public MeV1GetEndpointResponse MapFrom(MeV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MeV1GetEndpointResponse
        {
            UserId = input.UserId,
            Username = input.Username,
            Email = input.Email,
            Roles = input.Roles,
            IsEnabled = input.IsEnabled,
            CreatedAt = input.CreatedAt,
            PartnerSlug = input.PartnerSlug,
            Links = []
        };
    }
}
