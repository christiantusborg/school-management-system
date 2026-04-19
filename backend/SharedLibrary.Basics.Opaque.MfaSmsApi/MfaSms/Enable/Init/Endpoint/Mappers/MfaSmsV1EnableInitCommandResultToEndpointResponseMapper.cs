using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Init.Endpoint.Mappers;

public class MfaSmsV1EnableInitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaSmsV1EnableInitCommandResult, MfaSmsV1EnableInitEndpointResponse>
{
    public MfaSmsV1EnableInitEndpointResponse MapFrom(MfaSmsV1EnableInitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaSmsV1EnableInitEndpointResponse
        {
            SessionId = input.SessionId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.SessionId)
        };
    }
}
