using School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Endpoint.Mappers;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class FinalProjectStatusV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<FinalProjectStatusV1RestoreCommandResult, FinalProjectStatusV1RestoreEndpointResponse>,
            FinalProjectStatusV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}
