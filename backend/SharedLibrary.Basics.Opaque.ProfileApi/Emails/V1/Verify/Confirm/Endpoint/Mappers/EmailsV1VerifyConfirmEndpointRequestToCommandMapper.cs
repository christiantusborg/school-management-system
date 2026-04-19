using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Endpoint.Mappers;

public class EmailsV1VerifyConfirmEndpointRequestToCommandMapper
    : IMapper<EmailsV1VerifyConfirmEndpointRequest, EmailsV1VerifyConfirmCommand>
{
    public EmailsV1VerifyConfirmCommand MapFrom(EmailsV1VerifyConfirmEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new EmailsV1VerifyConfirmCommand
        {
            SessionId = input.SessionId,
            Code = input.Code,
        };
    }
}
