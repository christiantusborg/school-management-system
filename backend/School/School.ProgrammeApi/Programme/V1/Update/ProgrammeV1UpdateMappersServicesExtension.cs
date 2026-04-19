using School.ProgrammeApi.Programme.V1.Update.Command;
using School.ProgrammeApi.Programme.V1.Update.Endpoint;
using School.ProgrammeApi.Programme.V1.Update.Endpoint.Mappers;

namespace School.ProgrammeApi.Programme.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ProgrammeV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ProgrammeV1UpdateEndpointRequest, ProgrammeV1UpdateCommand>,
            ProgrammeV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<ProgrammeV1UpdateCommandResult, ProgrammeV1UpdateEndpointResponse>,
            ProgrammeV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
