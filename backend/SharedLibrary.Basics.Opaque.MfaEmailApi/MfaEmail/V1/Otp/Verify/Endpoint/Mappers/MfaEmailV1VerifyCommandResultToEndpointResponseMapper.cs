using SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Command;

namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Otp.Verify.Endpoint.Mappers;

public class MfaEmailV1VerifyCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<MfaEmailV1VerifyCommandResult, MfaEmailV1VerifyEndpointResponse>
{
    public MfaEmailV1VerifyEndpointResponse MapFrom(MfaEmailV1VerifyCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new MfaEmailV1VerifyEndpointResponse
        {
            Token = input.Token,
            ExpiresAt = input.ExpiresAt,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, Guid.Empty)
        };
    }
}
