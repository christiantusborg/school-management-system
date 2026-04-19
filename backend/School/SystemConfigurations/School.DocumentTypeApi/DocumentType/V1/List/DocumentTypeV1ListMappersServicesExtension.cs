using School.DocumentTypeApi.DocumentType.V1.List.Command;
using School.DocumentTypeApi.DocumentType.V1.List.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.List.Endpoint.Mappers;

namespace School.DocumentTypeApi.DocumentType.V1.List;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class DocumentTypeV1ListMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<
            IMapper<CommandSearchResult<DocumentTypeV1ListCommandResultItem>, BaseGetAllResponse<DocumentTypeV1ListEndpointResponseItem>>,
            DocumentTypeV1ListCommandResultToEndpointResponseMapper>();
        return services;
    }
}
