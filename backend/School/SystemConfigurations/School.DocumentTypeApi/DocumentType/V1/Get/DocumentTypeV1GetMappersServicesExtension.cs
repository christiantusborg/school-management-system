using School.DocumentTypeApi.DocumentType.V1.Get.Command;
using School.DocumentTypeApi.DocumentType.V1.Get.Endpoint;
using School.DocumentTypeApi.DocumentType.V1.Get.Endpoint.Mappers;

namespace School.DocumentTypeApi.DocumentType.V1.Get;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed class DocumentTypeV1GetMappersServicesExtension : IMapperMarker
{
    public IServiceCollection Map(IServiceCollection services)
    {
        services.TryAddSingleton<IMapper<DocumentTypeV1GetCommandResult, DocumentTypeV1GetEndpointResponse>,
            DocumentTypeV1GetCommandResultToEndpointResponseMapper>();
        return services;
    }
}
