using School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Command;
using School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Endpoint;

namespace School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Endpoint.Mappers;

public sealed class DocumentTypeV1PermanentDeleteCommandResultToEndpointResponseMapper
    : IMapper<DocumentTypeV1PermanentDeleteCommandResult, DocumentTypeV1PermanentDeleteEndpointResponse>
{
    public DocumentTypeV1PermanentDeleteEndpointResponse MapFrom(DocumentTypeV1PermanentDeleteCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new DocumentTypeV1PermanentDeleteEndpointResponse
        {
            DocumentTypeId = input.DocumentTypeId,
            Links = []
        };
    }
}
