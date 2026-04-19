using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Verify.Confirm;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class EmailsV1VerifyConfirmMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EmailsV1VerifyConfirmCommandResult, EmailsV1VerifyConfirmEndpointResponse>,
            EmailsV1VerifyConfirmCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<EmailsV1VerifyConfirmEndpointRequest, EmailsV1VerifyConfirmCommand>,
            EmailsV1VerifyConfirmEndpointRequestToCommandMapper>();
        return services;
    }
}
