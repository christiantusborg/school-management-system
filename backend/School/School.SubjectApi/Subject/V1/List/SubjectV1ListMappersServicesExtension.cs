using School.SubjectApi.Subject.V1.List.Command;
using School.SubjectApi.Subject.V1.List.Endpoint;
using School.SubjectApi.Subject.V1.List.Endpoint.Mappers;

namespace School.SubjectApi.Subject.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class SubjectV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<SubjectV1ListCommandResultItem>, BaseGetAllResponse<SubjectV1ListEndpointResponseItem>>,
            SubjectV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
