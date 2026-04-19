using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Delete.Endpoint.Mappers;

public class MfaTotpV1DeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaTotpV1DeleteCommandResult, MfaTotpV1DeleteEndpointResponse>
{
    public MfaTotpV1DeleteEndpointResponse MapFrom(MfaTotpV1DeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaTotpV1DeleteEndpointResponse
        {
            UserTwoFactorMethodId = input.UserTwoFactorMethodId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserTwoFactorMethodId)
        };
    }
}
