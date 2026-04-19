using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint.Mappers;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class FinalProjectStatusV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<FinalProjectStatusV1UpdateEndpointRequest, FinalProjectStatusV1UpdateCommand>,
            FinalProjectStatusV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<FinalProjectStatusV1UpdateCommandResult, FinalProjectStatusV1UpdateEndpointResponse>,
            FinalProjectStatusV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
