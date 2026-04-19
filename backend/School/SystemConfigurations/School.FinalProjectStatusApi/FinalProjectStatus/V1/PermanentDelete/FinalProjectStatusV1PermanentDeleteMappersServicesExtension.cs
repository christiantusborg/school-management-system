using School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Endpoint.Mappers;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class FinalProjectStatusV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<FinalProjectStatusV1PermanentDeleteCommandResult, FinalProjectStatusV1PermanentDeleteEndpointResponse>,
            FinalProjectStatusV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
