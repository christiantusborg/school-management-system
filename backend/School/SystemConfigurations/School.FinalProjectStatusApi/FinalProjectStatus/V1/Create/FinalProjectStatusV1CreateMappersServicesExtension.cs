using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint.Mappers;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class FinalProjectStatusV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<FinalProjectStatusV1CreateEndpointRequest, FinalProjectStatusV1CreateCommand>,
            FinalProjectStatusV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<FinalProjectStatusV1CreateCommandResult, FinalProjectStatusV1CreateEndpointResponse>,
            FinalProjectStatusV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
