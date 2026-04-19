using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Endpoint.Mappers;

public class EmailsV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<EmailsV1GetCommandResult, EmailsV1GetEndpointResponse>
{
    public EmailsV1GetEndpointResponse MapFrom(EmailsV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1GetEndpointResponse
        {
            UserContactEmailId = input.UserContactEmailId,
            Email = input.Email,
            Label = input.Label,
            IsPrimary = input.IsPrimary,
            Links = HateoasLinksHelper.AsGet(httpContextAccessor, input.UserContactEmailId)
        };
    }
}
