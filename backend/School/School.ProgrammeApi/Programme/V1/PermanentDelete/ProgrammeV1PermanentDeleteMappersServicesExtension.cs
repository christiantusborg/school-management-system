using School.ProgrammeApi.Programme.V1.PermanentDelete.Command;
using School.ProgrammeApi.Programme.V1.PermanentDelete.Endpoint;
using School.ProgrammeApi.Programme.V1.PermanentDelete.Endpoint.Mappers;

namespace School.ProgrammeApi.Programme.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ProgrammeV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ProgrammeV1PermanentDeleteCommandResult, ProgrammeV1PermanentDeleteEndpointResponse>,
            ProgrammeV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
