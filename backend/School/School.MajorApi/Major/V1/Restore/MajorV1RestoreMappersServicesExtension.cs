using School.MajorApi.Major.V1.Restore.Command;
using School.MajorApi.Major.V1.Restore.Endpoint;
using School.MajorApi.Major.V1.Restore.Endpoint.Mappers;

namespace School.MajorApi.Major.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class MajorV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<MajorV1RestoreCommandResult, MajorV1RestoreEndpointResponse>,
            MajorV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}
