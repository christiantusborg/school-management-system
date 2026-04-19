using School.DocumentTypeApi.DocumentType.V1.Create.Command;
using School.DocumentTypeApi.DocumentType.V1.Create.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.Create.Endpoint.Mappers;

namespace School.DocumentTypeApi.DocumentType.V1.Create;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class DocumentTypeV1CreateMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<DocumentTypeV1CreateEndpointRequest, DocumentTypeV1CreateCommand>,
            DocumentTypeV1CreateEndpointRequestToCommandMapper>();
        services.TryAddSingleton<IMapper<DocumentTypeV1CreateCommandResult, DocumentTypeV1CreateEndpointResponse>,
            DocumentTypeV1CreateCommandResultToEndpointResponseMapper>();
        return services;
    }
}
