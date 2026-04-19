using School.PathwayApi.Pathway.V1.Get.Command;
using School.PathwayApi.Pathway.V1.Get.Endpoint;
using School.PathwayApi.Pathway.V1.Get.Endpoint.Mappers;

namespace School.PathwayApi.Pathway.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PathwayV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PathwayV1GetCommandResult, PathwayV1GetEndpointResponse>,
            PathwayV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
