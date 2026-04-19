using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Init.Endpoint.Mappers;

public class MfaTotpV1EnableInitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaTotpV1EnableInitCommandResult, MfaTotpV1EnableInitEndpointResponse>
{
    public MfaTotpV1EnableInitEndpointResponse MapFrom(MfaTotpV1EnableInitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaTotpV1EnableInitEndpointResponse
        {
            Secret = input.Secret,
            QrUri = input.QrUri,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
