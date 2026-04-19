using SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaToTpApi.MfaTotp.V1.Otp.Verify.Endpoint.Mappers;

public class MfaTotpV1VerifyCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaTotpV1VerifyCommandResult, MfaTotpV1VerifyEndpointResponse>
{
    public MfaTotpV1VerifyEndpointResponse MapFrom(MfaTotpV1VerifyCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaTotpV1VerifyEndpointResponse
        {
            Token = input.Token,
            ExpiresAt = input.ExpiresAt,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
