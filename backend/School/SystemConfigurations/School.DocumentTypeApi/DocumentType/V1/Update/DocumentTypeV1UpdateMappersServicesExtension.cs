using School.DocumentTypeApi.DocumentType.V1.Update.Command;
using School.DocumentTypeApi.DocumentType.V1.Update.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.Update.Endpoint.Mappers;

namespace School.DocumentTypeApi.DocumentType.V1.Update;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class DocumentTypeV1UpdateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<DocumentTypeV1UpdateEndpointRequest, DocumentTypeV1UpdateCommand>,
            DocumentTypeV1UpdateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<DocumentTypeV1UpdateCommandResult, DocumentTypeV1UpdateEndpointResponse>,
            DocumentTypeV1UpdateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
