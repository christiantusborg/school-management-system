using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Send.Endpoint.Mappers;

public class MfaSmsV1SendCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaSmsV1SendCommandResult, MfaSmsV1SendEndpointResponse>
{
    public MfaSmsV1SendEndpointResponse MapFrom(MfaSmsV1SendCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaSmsV1SendEndpointResponse
        {
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
