using SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Otp.Verify.Endpoint.Mappers;

public class MfaSmsV1VerifyCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaSmsV1VerifyCommandResult, MfaSmsV1VerifyEndpointResponse>
{
    public MfaSmsV1VerifyEndpointResponse MapFrom(MfaSmsV1VerifyCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaSmsV1VerifyEndpointResponse
        {
            Token = input.Token,
            ExpiresAt = input.ExpiresAt,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
