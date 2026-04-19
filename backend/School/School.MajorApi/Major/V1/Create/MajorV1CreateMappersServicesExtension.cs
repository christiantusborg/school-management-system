using School.MajorApi.Major.V1.Create.Command;
using School.MajorApi.Major.V1.Create.Endpoint;
using School.MajorApi.Major.V1.Create.Endpoint.Mappers;

namespace School.MajorApi.Major.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MajorV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MajorV1CreateEndpointRequest, MajorV1CreateCommand>,
            MajorV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<MajorV1CreateCommandResult, MajorV1CreateEndpointResponse>,
            MajorV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
