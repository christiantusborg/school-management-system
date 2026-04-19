using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class EmailsV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<EmailsV1GetCommandResult, EmailsV1GetEndpointResponse>,
            EmailsV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
