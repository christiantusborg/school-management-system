using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Endpoint.Mappers;

public class EmailsV1DeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EmailsV1DeleteCommandResult, EmailsV1DeleteEndpointResponse>
{
    public EmailsV1DeleteEndpointResponse MapFrom(EmailsV1DeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1DeleteEndpointResponse
        {
            UserContactEmailId = input.UserContactEmailId,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserContactEmailId)
        };
    }
}
