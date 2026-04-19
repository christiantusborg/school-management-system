using School.DocumentTypeApi.DocumentType.V1.Restore.Command;
using School.DocumentTypeApi.DocumentType.V1.Restore.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.Restore.Endpoint.Mappers;

namespace School.DocumentTypeApi.DocumentType.V1.Restore;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class DocumentTypeV1RestoreMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<DocumentTypeV1RestoreCommandResult, DocumentTypeV1RestoreEndpointResponse>,
            DocumentTypeV1RestoreCommandResultToEndpointResponseMapper>();
        return services;
    }
}
