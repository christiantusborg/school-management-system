using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.SetPrimary;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class EmailsV1SetPrimaryMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EmailsV1SetPrimaryCommandResult, EmailsV1SetPrimaryEndpointResponse>,
            EmailsV1SetPrimaryCommandResultToEndpointResponseMapper>();
        return services;
    }
}
