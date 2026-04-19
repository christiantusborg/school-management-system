using School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Endpoint.Mappers;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class FinalProjectStatusV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<FinalProjectStatusV1GetCommandResult, FinalProjectStatusV1GetEndpointResponse>,
            FinalProjectStatusV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
