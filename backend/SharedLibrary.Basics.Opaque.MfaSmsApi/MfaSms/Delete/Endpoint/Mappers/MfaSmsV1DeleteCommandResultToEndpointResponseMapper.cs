using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Delete.Endpoint.Mappers;

public class MfaSmsV1DeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaSmsV1DeleteCommandResult, MfaSmsV1DeleteEndpointResponse>
{
    public MfaSmsV1DeleteEndpointResponse MapFrom(MfaSmsV1DeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaSmsV1DeleteEndpointResponse
        {
            UserTwoFactorMethodId = input.UserTwoFactorMethodId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserTwoFactorMethodId)
        };
    }
}
