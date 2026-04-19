using School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Command;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Endpoint;
using School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Endpoint.Mappers;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class FinalProjectStatusV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<FinalProjectStatusV1ListCommandResultItem>, BaseGetAllResponse<FinalProjectStatusV1ListEndpointResponseItem>>,
            FinalProjectStatusV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
