using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Endpoint.Mappers;

public class PhonesV1SetPrimaryCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PhonesV1SetPrimaryCommandResult, PhonesV1SetPrimaryEndpointResponse>
{
    public PhonesV1SetPrimaryEndpointResponse MapFrom(PhonesV1SetPrimaryCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1SetPrimaryEndpointResponse
        {
            UserPhoneId = input.UserPhoneId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserPhoneId)
        };
    }
}
