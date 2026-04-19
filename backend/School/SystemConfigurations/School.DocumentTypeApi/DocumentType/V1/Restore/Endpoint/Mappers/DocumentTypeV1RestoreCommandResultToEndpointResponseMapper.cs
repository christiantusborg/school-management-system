using School.DocumentTypeApi.DocumentType.V1.Restore.Command;
using School.DocumentTypeApi.DocumentType.V1.Restore.Endpoint;

namespace School.DocumentTypeApi.DocumentType.V1.Restore.Endpoint.Mappers;

public sealed class DocumentTypeV1RestoreCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<DocumentTypeV1RestoreCommandResult, DocumentTypeV1RestoreEndpointResponse>
{
    public DocumentTypeV1RestoreEndpointResponse MapFrom(DocumentTypeV1RestoreCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new DocumentTypeV1RestoreEndpointResponse
        {
            DocumentTypeId = input.DocumentTypeId,
            Links = []
        };
    }
}
