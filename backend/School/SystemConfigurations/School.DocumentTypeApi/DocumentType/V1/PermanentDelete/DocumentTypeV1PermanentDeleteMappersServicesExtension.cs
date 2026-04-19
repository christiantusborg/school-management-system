using School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Command;
using School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Endpoint.Mappers;

namespace School.DocumentTypeApi.DocumentType.V1.PermanentDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class DocumentTypeV1PermanentDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<DocumentTypeV1PermanentDeleteCommandResult, DocumentTypeV1PermanentDeleteEndpointResponse>,
            DocumentTypeV1PermanentDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
