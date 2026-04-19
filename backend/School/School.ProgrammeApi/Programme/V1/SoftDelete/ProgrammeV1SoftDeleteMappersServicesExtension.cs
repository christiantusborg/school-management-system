using School.ProgrammeApi.Programme.V1.SoftDelete.Command;
using School.ProgrammeApi.Programme.V1.SoftDelete.Endpoint;
using School.ProgrammeApi.Programme.V1.SoftDelete.Endpoint.Mappers;

namespace School.ProgrammeApi.Programme.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ProgrammeV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ProgrammeV1SoftDeleteCommandResult, ProgrammeV1SoftDeleteEndpointResponse>,
            ProgrammeV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
