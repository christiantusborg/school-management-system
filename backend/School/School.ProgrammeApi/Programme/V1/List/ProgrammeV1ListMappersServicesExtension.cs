using School.ProgrammeApi.Programme.V1.List.Command;
using School.ProgrammeApi.Programme.V1.List.Endpoint;
using School.ProgrammeApi.Programme.V1.List.Endpoint.Mappers;

namespace School.ProgrammeApi.Programme.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ProgrammeV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<ProgrammeV1ListCommandResultItem>, BaseGetAllResponse<ProgrammeV1ListEndpointResponseItem>>,
            ProgrammeV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
