using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Command;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Endpoint;
using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Endpoint.Mappers;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class TuitionFeeStatusV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<TuitionFeeStatusV1UpdateEndpointRequest, TuitionFeeStatusV1UpdateCommand>,
            TuitionFeeStatusV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<TuitionFeeStatusV1UpdateCommandResult, TuitionFeeStatusV1UpdateEndpointResponse>,
            TuitionFeeStatusV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
