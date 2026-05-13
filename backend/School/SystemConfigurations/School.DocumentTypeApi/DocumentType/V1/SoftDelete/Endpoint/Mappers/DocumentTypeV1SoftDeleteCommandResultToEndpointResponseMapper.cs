using School.DocumentTypeApi.DocumentType.V1.SoftDelete.Command;

namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete.Endpoint.Mappers;

public sealed class DocumentTypeV1SoftDeleteCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<DocumentTypeV1SoftDeleteCommandResult, DocumentTypeV1SoftDeleteEndpointResponse>
{
    public DocumentTypeV1SoftDeleteEndpointResponse MapFrom(DocumentTypeV1SoftDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new DocumentTypeV1SoftDeleteEndpointResponse
        {
            DocumentTypeId = input.DocumentTypeId,
            Links = []
        };
    }
}
