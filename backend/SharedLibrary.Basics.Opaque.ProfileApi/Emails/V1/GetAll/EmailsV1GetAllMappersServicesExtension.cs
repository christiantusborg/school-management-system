using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Command;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Endpoint;
using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll.Endpoint.Mappers;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.GetAll;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class EmailsV1GetAllMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<CommandSearchResult<EmailsV1GetAllCommandResultItem>, BaseGetAllResponse<EmailsV1GetAllEndpointResponseItem>>,
            EmailsV1GetAllCommandResultToEndpointResponseMapper>();
        return services;
    }
}
