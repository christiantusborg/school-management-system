using School.DocumentTypeApi.DocumentType.V1.Update.Command;

namespace School.DocumentTypeApi.DocumentType.V1.Update.Endpoint.Mappers;

public sealed class DocumentTypeV1UpdateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<DocumentTypeV1UpdateCommandResult, DocumentTypeV1UpdateEndpointResponse>
{
    public DocumentTypeV1UpdateEndpointResponse MapFrom(DocumentTypeV1UpdateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new DocumentTypeV1UpdateEndpointResponse
        {
            DocumentTypeId = input.DocumentTypeId,
            Links = []
        };
    }
}
