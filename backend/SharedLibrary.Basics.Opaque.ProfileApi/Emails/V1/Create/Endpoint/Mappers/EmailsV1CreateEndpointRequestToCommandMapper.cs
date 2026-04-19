using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Endpoint.Mappers;

public class EmailsV1CreateEndpointRequestToCommandMapper
    : IMapper<EmailsV1CreateEndpointRequest, EmailsV1CreateCommand>
{
    public EmailsV1CreateCommand MapFrom(EmailsV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1CreateCommand
        {
            UserId = string.Empty, // overwritten in endpoint
            Email = input.Email,
            Label = input.Label,
        };
    }
}
