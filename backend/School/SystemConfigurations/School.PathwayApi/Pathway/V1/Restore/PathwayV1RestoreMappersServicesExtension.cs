using School.PathwayApi.Pathway.V1.Restore.Command;
using School.PathwayApi.Pathway.V1.Restore.Endpoint;
using School.PathwayApi.Pathway.V1.Restore.Endpoint.Mappers;

namespace School.PathwayApi.Pathway.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PathwayV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PathwayV1RestoreCommandResult, PathwayV1RestoreEndpointResponse>,
            PathwayV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}
