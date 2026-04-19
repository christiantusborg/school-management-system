using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Endpoint.Mappers;

public class MfaFido2V1AssertionInitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaFido2V1AssertionInitCommandResult, MfaFido2V1AssertionInitEndpointResponse>
{
    public MfaFido2V1AssertionInitEndpointResponse MapFrom(MfaFido2V1AssertionInitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaFido2V1AssertionInitEndpointResponse
        {
            OptionsJson = input.OptionsJson,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
