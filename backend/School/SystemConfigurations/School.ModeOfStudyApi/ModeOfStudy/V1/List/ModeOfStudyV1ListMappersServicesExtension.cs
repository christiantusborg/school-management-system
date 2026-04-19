using School.ModeOfStudyApi.ModeOfStudy.V1.List.Command;
using School.ModeOfStudyApi.ModeOfStudy.V1.List.Endpoint;
using School.ModeOfStudyApi.ModeOfStudy.V1.List.Endpoint.Mappers;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class ModeOfStudyV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<ModeOfStudyV1ListCommandResultItem>, BaseGetAllResponse<ModeOfStudyV1ListEndpointResponseItem>>,
            ModeOfStudyV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
