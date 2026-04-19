using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Init.Endpoint.Mappers;

public class MfaEmailV1EnableInitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaEmailV1EnableInitCommandResult, MfaEmailV1EnableInitEndpointResponse>
{
    public MfaEmailV1EnableInitEndpointResponse MapFrom(MfaEmailV1EnableInitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaEmailV1EnableInitEndpointResponse
        {
            SessionId = input.SessionId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.SessionId)
        };
    }
}
