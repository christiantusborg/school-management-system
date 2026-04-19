using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Init.Endpoint.Mappers;

public class EmailsV1VerifyInitCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EmailsV1VerifyInitCommandResult, EmailsV1VerifyInitEndpointResponse>
{
    public EmailsV1VerifyInitEndpointResponse MapFrom(EmailsV1VerifyInitCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1VerifyInitEndpointResponse
        {
            SessionId = input.SessionId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.SessionId)
        };
    }
}
