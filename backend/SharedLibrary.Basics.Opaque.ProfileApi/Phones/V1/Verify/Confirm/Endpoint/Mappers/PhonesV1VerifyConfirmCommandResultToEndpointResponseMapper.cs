using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Endpoint.Mappers;

public class PhonesV1VerifyConfirmCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<PhonesV1VerifyConfirmCommandResult, PhonesV1VerifyConfirmEndpointResponse>
{
    public PhonesV1VerifyConfirmEndpointResponse MapFrom(PhonesV1VerifyConfirmCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new PhonesV1VerifyConfirmEndpointResponse
        {
            UserPhoneId = input.UserPhoneId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserPhoneId)
        };
    }
}
