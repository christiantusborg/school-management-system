using SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Endpoint.Mappers;

public class MfaFido2V1DeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaFido2V1DeleteCommandResult, MfaFido2V1DeleteEndpointResponse>
{
    public MfaFido2V1DeleteEndpointResponse MapFrom(MfaFido2V1DeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaFido2V1DeleteEndpointResponse
        {
            Fido2CredentialId = input.Fido2CredentialId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
