using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Get.Endpoint.Mappers;

public class PhonesV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PhonesV1GetCommandResult, PhonesV1GetEndpointResponse>
{
    public PhonesV1GetEndpointResponse MapFrom(PhonesV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1GetEndpointResponse
        {
            UserPhoneId = input.UserPhoneId,
            Number = input.Number,
            Label = input.Label,
            IsPrimary = input.IsPrimary,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserPhoneId)
        };
    }
}
