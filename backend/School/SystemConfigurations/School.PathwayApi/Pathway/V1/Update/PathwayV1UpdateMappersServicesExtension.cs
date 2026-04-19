using School.PathwayApi.Pathway.V1.Update.Command;
using School.PathwayApi.Pathway.V1.Update.Endpoint;
using School.PathwayApi.Pathway.V1.Update.Endpoint.Mappers;

namespace School.PathwayApi.Pathway.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class PathwayV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<PathwayV1UpdateEndpointRequest, PathwayV1UpdateCommand>,
            PathwayV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<PathwayV1UpdateCommandResult, PathwayV1UpdateEndpointResponse>,
            PathwayV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
