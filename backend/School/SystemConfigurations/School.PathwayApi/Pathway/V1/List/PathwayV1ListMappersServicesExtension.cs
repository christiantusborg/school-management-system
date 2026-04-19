using School.PathwayApi.Pathway.V1.List.Command;
using School.PathwayApi.Pathway.V1.List.Endpoint;
using School.PathwayApi.Pathway.V1.List.Endpoint.Mappers;

namespace School.PathwayApi.Pathway.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PathwayV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<PathwayV1ListCommandResultItem>, BaseGetAllResponse<PathwayV1ListEndpointResponseItem>>,
            PathwayV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
