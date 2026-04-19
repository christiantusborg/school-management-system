using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Endpoint.Mappers;

public class MfaSmsV1EnableConfirmCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaSmsV1EnableConfirmCommandResult, MfaSmsV1EnableConfirmEndpointResponse>
{
    public MfaSmsV1EnableConfirmEndpointResponse MapFrom(MfaSmsV1EnableConfirmCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaSmsV1EnableConfirmEndpointResponse
        {
            UserTwoFactorMethodId = input.UserTwoFactorMethodId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserTwoFactorMethodId)
        };
    }
}
