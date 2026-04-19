using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Enable.Confirm.Endpoint.Mappers;

public class MfaTotpV1EnableConfirmCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaTotpV1EnableConfirmCommandResult, MfaTotpV1EnableConfirmEndpointResponse>
{
    public MfaTotpV1EnableConfirmEndpointResponse MapFrom(MfaTotpV1EnableConfirmCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaTotpV1EnableConfirmEndpointResponse
        {
            UserTwoFactorMethodId = input.UserTwoFactorMethodId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserTwoFactorMethodId)
        };
    }
}
