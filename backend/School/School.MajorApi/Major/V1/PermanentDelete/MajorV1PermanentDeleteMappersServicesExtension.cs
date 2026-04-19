using School.MajorApi.Major.V1.PermanentDelete.Command;
using School.MajorApi.Major.V1.PermanentDelete.Endpoint;
using School.MajorApi.Major.V1.PermanentDelete.Endpoint.Mappers;

namespace School.MajorApi.Major.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MajorV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MajorV1PermanentDeleteCommandResult, MajorV1PermanentDeleteEndpointResponse>,
            MajorV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
