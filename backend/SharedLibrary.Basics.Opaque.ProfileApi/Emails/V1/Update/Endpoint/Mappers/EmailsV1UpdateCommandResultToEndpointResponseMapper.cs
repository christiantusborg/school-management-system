using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Endpoint.Mappers;

public class EmailsV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EmailsV1UpdateCommandResult, EmailsV1UpdateEndpointResponse>
{
    public EmailsV1UpdateEndpointResponse MapFrom(EmailsV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1UpdateEndpointResponse
        {
            UserContactEmailId = input.UserContactEmailId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserContactEmailId)
        };
    }
}
