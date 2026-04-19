using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Endpoint.Mappers;

public class MfaFido2V1RegisterFinishCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaFido2V1RegisterFinishCommandResult, MfaFido2V1RegisterFinishEndpointResponse>
{
    public MfaFido2V1RegisterFinishEndpointResponse MapFrom(MfaFido2V1RegisterFinishCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaFido2V1RegisterFinishEndpointResponse
        {
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
