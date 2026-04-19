using School.DocumentTypeApi.DocumentType.V1.Create.Command;
using School.DocumentTypeApi.DocumentType.V1.Create.Endpoint;

namespace School.DocumentTypeApi.DocumentType.V1.Create.Endpoint.Mappers;

public sealed class DocumentTypeV1CreateCommandResultToEndpointResponseMapper(IHttpContextAccessor httpContextAccessor)
    : IMapper<DocumentTypeV1CreateCommandResult, DocumentTypeV1CreateEndpointResponse>
{
    public DocumentTypeV1CreateEndpointResponse MapFrom(DocumentTypeV1CreateCommandResult input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new DocumentTypeV1CreateEndpointResponse
        {
            DocumentTypeId = input.DocumentTypeId,
            Links = []
        };
    }
}
