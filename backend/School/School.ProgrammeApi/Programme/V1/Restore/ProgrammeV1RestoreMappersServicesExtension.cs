using School.ProgrammeApi.Programme.V1.Restore.Command;
using School.ProgrammeApi.Programme.V1.Restore.Endpoint;
using School.ProgrammeApi.Programme.V1.Restore.Endpoint.Mappers;

namespace School.ProgrammeApi.Programme.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ProgrammeV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ProgrammeV1RestoreCommandResult, ProgrammeV1RestoreEndpointResponse>,
            ProgrammeV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}
