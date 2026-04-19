using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class EmailsV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EmailsV1UpdateCommandResult, EmailsV1UpdateEndpointResponse>,
            EmailsV1UpdateCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<EmailsV1UpdateEndpointRequest, EmailsV1UpdateCommand>,
            EmailsV1UpdateEndpointRequestToCommandMapper>();
        return services;
    }
}
