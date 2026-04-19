using School.ProgrammeApi.Programme.V1.Get.Command;
using School.ProgrammeApi.Programme.V1.Get.Endpoint;
using School.ProgrammeApi.Programme.V1.Get.Endpoint.Mappers;

namespace School.ProgrammeApi.Programme.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ProgrammeV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<ProgrammeV1GetCommandResult, ProgrammeV1GetEndpointResponse>,
            ProgrammeV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
