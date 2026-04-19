using School.MajorApi.Major.V1.List.Command;
using School.MajorApi.Major.V1.List.Endpoint;
using School.MajorApi.Major.V1.List.Endpoint.Mappers;

namespace School.MajorApi.Major.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MajorV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<MajorV1ListCommandResultItem>, BaseGetAllResponse<MajorV1ListEndpointResponseItem>>,
            MajorV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
