using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Endpoint.Mappers;

public class EmailsV1SetPrimaryCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EmailsV1SetPrimaryCommandResult, EmailsV1SetPrimaryEndpointResponse>
{
    public EmailsV1SetPrimaryEndpointResponse MapFrom(EmailsV1SetPrimaryCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1SetPrimaryEndpointResponse
        {
            UserContactEmailId = input.UserContactEmailId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserContactEmailId)
        };
    }
}
