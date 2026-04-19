using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Endpoint.Mappers;

public class EmailsV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EmailsV1CreateCommandResult, EmailsV1CreateEndpointResponse>
{
    public EmailsV1CreateEndpointResponse MapFrom(EmailsV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1CreateEndpointResponse
        {
            UserContactEmailId = input.UserContactEmailId,
            Links = HateoasLinksHelper.AsCreate(httpContextAccessor, input.UserContactEmailId)
        };
    }
}
