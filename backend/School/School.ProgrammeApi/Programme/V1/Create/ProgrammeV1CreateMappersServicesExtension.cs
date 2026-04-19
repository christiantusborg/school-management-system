using School.ProgrammeApi.Programme.V1.Create.Command;
using School.ProgrammeApi.Programme.V1.Create.Endpoint;
using School.ProgrammeApi.Programme.V1.Create.Endpoint.Mappers;

namespace School.ProgrammeApi.Programme.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ProgrammeV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ProgrammeV1CreateEndpointRequest, ProgrammeV1CreateCommand>,
            ProgrammeV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<ProgrammeV1CreateCommandResult, ProgrammeV1CreateEndpointResponse>,
            ProgrammeV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
