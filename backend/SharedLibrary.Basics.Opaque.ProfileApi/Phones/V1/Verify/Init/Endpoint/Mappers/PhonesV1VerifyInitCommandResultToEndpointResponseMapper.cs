using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Endpoint.Mappers;

public class PhonesV1VerifyInitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PhonesV1VerifyInitCommandResult, PhonesV1VerifyInitEndpointResponse>
{
    public PhonesV1VerifyInitEndpointResponse MapFrom(PhonesV1VerifyInitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1VerifyInitEndpointResponse
        {
            SessionId = input.SessionId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.SessionId)
        };
    }
}
