using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Endpoint.Mappers;

public class MfaFido2V1RegisterInitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaFido2V1RegisterInitCommandResult, MfaFido2V1RegisterInitEndpointResponse>
{
    public MfaFido2V1RegisterInitEndpointResponse MapFrom(MfaFido2V1RegisterInitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaFido2V1RegisterInitEndpointResponse
        {
            OptionsJson = input.OptionsJson,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
