using School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Endpoint.Mappers;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class FinalProjectStatusV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<FinalProjectStatusV1SoftDeleteCommandResult, FinalProjectStatusV1SoftDeleteEndpointResponse>,
            FinalProjectStatusV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
