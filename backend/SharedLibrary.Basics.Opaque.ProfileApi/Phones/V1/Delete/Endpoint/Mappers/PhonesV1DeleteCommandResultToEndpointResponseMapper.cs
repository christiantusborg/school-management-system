using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Endpoint.Mappers;

public class PhonesV1DeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PhonesV1DeleteCommandResult, PhonesV1DeleteEndpointResponse>
{
    public PhonesV1DeleteEndpointResponse MapFrom(PhonesV1DeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1DeleteEndpointResponse
        {
            UserPhoneId = input.UserPhoneId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserPhoneId)
        };
    }
}
