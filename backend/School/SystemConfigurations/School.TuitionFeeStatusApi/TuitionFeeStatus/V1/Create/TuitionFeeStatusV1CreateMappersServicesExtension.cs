using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Endpoint.Mappers;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class TuitionFeeStatusV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<TuitionFeeStatusV1CreateEndpointRequest, TuitionFeeStatusV1CreateCommand>,
            TuitionFeeStatusV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<TuitionFeeStatusV1CreateCommandResult, TuitionFeeStatusV1CreateEndpointResponse>,
            TuitionFeeStatusV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
