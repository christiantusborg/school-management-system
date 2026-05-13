using School.PathwayApi.EducationLevel.V1.List.Command;
using School.PathwayApi.EducationLevel.V1.List.Endpoint;
using School.PathwayApi.EducationLevel.V1.List.Endpoint.Mappers;

namespace School.PathwayApi.EducationLevel.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class EducationLevelV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<EducationLevelV1ListCommandResultItem>, BaseGetAllResponse<EducationLevelV1ListEndpointResponseItem>>,
            EducationLevelV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
