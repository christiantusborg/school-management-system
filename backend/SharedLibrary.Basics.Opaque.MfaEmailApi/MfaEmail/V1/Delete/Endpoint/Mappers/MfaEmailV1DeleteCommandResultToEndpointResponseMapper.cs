using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Delete.Endpoint.Mappers;

public class MfaEmailV1DeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaEmailV1DeleteCommandResult, MfaEmailV1DeleteEndpointResponse>
{
    public MfaEmailV1DeleteEndpointResponse MapFrom(MfaEmailV1DeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaEmailV1DeleteEndpointResponse
        {
            UserTwoFactorMethodId = input.UserTwoFactorMethodId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserTwoFactorMethodId)
        };
    }
}
