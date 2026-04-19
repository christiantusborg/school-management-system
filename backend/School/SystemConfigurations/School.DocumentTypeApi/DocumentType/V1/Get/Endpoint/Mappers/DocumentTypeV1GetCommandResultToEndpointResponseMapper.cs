using School.DocumentTypeApi.DocumentType.V1.Get.Command;
using School.DocumentTypeApi.DocumentType.V1.Get.Endpoint;

namespace School.DocumentTypeApi.DocumentType.V1.Get.Endpoint.Mappers;

public sealed class DocumentTypeV1GetCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<DocumentTypeV1GetCommandResult, DocumentTypeV1GetEndpointResponse>
{
    public DocumentTypeV1GetEndpointResponse MapFrom(DocumentTypeV1GetCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new DocumentTypeV1GetEndpointResponse
        {
            DocumentTypeId = input.DocumentTypeId,
            Name = input.Name,
            Description = input.Description,
            DeletedAt = input.DeletedAt,
            Links = []
        };
    }
}
