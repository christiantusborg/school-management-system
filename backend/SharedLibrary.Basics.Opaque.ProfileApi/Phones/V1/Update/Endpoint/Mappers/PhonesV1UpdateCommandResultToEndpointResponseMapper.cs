using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Endpoint.Mappers;

public class PhonesV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PhonesV1UpdateCommandResult, PhonesV1UpdateEndpointResponse>
{
    public PhonesV1UpdateEndpointResponse MapFrom(PhonesV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1UpdateEndpointResponse
        {
            UserPhoneId = input.UserPhoneId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserPhoneId)
        };
    }
}
