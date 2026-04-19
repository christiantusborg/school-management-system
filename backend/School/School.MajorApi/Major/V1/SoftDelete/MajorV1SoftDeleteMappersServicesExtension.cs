using School.MajorApi.Major.V1.SoftDelete.Command;
using School.MajorApi.Major.V1.SoftDelete.Endpoint;
using School.MajorApi.Major.V1.SoftDelete.Endpoint.Mappers;

namespace School.MajorApi.Major.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MajorV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MajorV1SoftDeleteCommandResult, MajorV1SoftDeleteEndpointResponse>,
            MajorV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
