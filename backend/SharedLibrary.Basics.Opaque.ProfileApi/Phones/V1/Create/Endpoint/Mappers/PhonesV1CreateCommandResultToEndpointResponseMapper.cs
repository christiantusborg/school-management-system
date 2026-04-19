using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Endpoint.Mappers;

public class PhonesV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PhonesV1CreateCommandResult, PhonesV1CreateEndpointResponse>
{
    public PhonesV1CreateEndpointResponse MapFrom(PhonesV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1CreateEndpointResponse
        {
            UserPhoneId = input.UserPhoneId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserPhoneId)
        };
    }
}
