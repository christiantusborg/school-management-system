using School.PathwayApi.Pathway.V1.Create.Command;
using School.PathwayApi.Pathway.V1.Create.Endpoint;
using School.PathwayApi.Pathway.V1.Create.Endpoint.Mappers;

namespace School.PathwayApi.Pathway.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PathwayV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PathwayV1CreateEndpointRequest, PathwayV1CreateCommand>,
            PathwayV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<PathwayV1CreateCommandResult, PathwayV1CreateEndpointResponse>,
            PathwayV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
