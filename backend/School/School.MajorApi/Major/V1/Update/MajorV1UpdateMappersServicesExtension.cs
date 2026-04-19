using School.MajorApi.Major.V1.Update.Command;
using School.MajorApi.Major.V1.Update.Endpoint;
using School.MajorApi.Major.V1.Update.Endpoint.Mappers;

namespace School.MajorApi.Major.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MajorV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MajorV1UpdateEndpointRequest, MajorV1UpdateCommand>,
            MajorV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<MajorV1UpdateCommandResult, MajorV1UpdateEndpointResponse>,
            MajorV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
