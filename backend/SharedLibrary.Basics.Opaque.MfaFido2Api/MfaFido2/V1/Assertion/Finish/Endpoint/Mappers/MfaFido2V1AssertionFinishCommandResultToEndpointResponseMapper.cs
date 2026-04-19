using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Endpoint.Mappers;

public class MfaFido2V1AssertionFinishCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaFido2V1AssertionFinishCommandResult, MfaFido2V1AssertionFinishEndpointResponse>
{
    public MfaFido2V1AssertionFinishEndpointResponse MapFrom(MfaFido2V1AssertionFinishCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaFido2V1AssertionFinishEndpointResponse
        {
            Token = input.Token,
            ExpiresAt = input.ExpiresAt,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
