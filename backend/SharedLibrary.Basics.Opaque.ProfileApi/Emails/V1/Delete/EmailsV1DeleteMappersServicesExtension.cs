using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Delete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class EmailsV1DeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EmailsV1DeleteCommandResult, EmailsV1DeleteEndpointResponse>,
            EmailsV1DeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
