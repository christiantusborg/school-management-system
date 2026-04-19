using School.DocumentTypeApi.DocumentType.V1.SoftDelete.Command;
using School.DocumentTypeApi.DocumentType.V1.SoftDelete.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.SoftDelete.Endpoint.Mappers;

namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class DocumentTypeV1SoftDeleteMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<DocumentTypeV1SoftDeleteCommandResult, DocumentTypeV1SoftDeleteEndpointResponse>,
            DocumentTypeV1SoftDeleteCommandResultToEndpointResponseMapper>();
        return services;
    }
}
