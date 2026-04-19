using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class EmailsV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EmailsV1CreateCommandResult, EmailsV1CreateEndpointResponse>,
            EmailsV1CreateCommandResultToEndpointResponseMapper>();
        services.TryAddSingleton<IMapper<EmailsV1CreateEndpointRequest, EmailsV1CreateCommand>,
            EmailsV1CreateEndpointRequestToCommandMapper>();
        return services;
    }
}
