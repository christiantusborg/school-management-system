using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Endpoint.Mappers;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class TuitionFeeStatusV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem>, BaseGetAllResponse<TuitionFeeStatusV1ListEndpointResponseItem>>,
            TuitionFeeStatusV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
