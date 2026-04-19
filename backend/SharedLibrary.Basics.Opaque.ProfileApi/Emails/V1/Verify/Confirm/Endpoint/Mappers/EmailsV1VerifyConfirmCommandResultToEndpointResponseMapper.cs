using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Endpoint.Mappers;

public class EmailsV1VerifyConfirmCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EmailsV1VerifyConfirmCommandResult, EmailsV1VerifyConfirmEndpointResponse>
{
    public EmailsV1VerifyConfirmEndpointResponse MapFrom(EmailsV1VerifyConfirmCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1VerifyConfirmEndpointResponse
        {
            UserContactEmailId = input.UserContactEmailId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserContactEmailId)
        };
    }
}
