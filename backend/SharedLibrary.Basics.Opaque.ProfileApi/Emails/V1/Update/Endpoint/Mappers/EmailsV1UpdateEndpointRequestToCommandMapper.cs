using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Endpoint.Mappers;

public class EmailsV1UpdateEndpointRequestToCommandMapper
    : IMapper<EmailsV1UpdateEndpointRequest, EmailsV1UpdateCommand>
{
    public EmailsV1UpdateCommand MapFrom(EmailsV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1UpdateCommand
        {
            UserContactEmailId = Guid.Empty, // overwritten in endpoint
            UserId = string.Empty, // overwritten in endpoint
            Email = input.Email,
            Label = input.Label,
        };
    }
}
