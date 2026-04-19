using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Endpoint.Mappers;

public class MfaEmailV1EnableConfirmCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaEmailV1EnableConfirmCommandResult, MfaEmailV1EnableConfirmEndpointResponse>
{
    public MfaEmailV1EnableConfirmEndpointResponse MapFrom(MfaEmailV1EnableConfirmCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaEmailV1EnableConfirmEndpointResponse
        {
            UserTwoFactorMethodId = input.UserTwoFactorMethodId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserTwoFactorMethodId)
        };
    }
}
