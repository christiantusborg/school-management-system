using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Send.Endpoint.Mappers;

public class MfaEmailV1SendCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaEmailV1SendCommandResult, MfaEmailV1SendEndpointResponse>
{
    public MfaEmailV1SendEndpointResponse MapFrom(MfaEmailV1SendCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaEmailV1SendEndpointResponse
        {
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
