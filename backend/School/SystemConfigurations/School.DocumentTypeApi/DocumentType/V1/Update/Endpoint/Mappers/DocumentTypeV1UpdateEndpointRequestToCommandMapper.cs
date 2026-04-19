using School.DocumentTypeApi.DocumentType.V1.Update.Command;
using School.DocumentTypeApi.DocumentType.V1.Update.Endpoint;

namespace School.DocumentTypeApi.DocumentType.V1.Update.Endpoint.Mappers;

public sealed class DocumentTypeV1UpdateEndpointRequestToCommandMapper
    : IMapper<DocumentTypeV1UpdateEndpointRequest, DocumentTypeV1UpdateCommand>
{
    public DocumentTypeV1UpdateCommand MapFrom(DocumentTypeV1UpdateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new DocumentTypeV1UpdateCommand
        {
            DocumentTypeId = 0, // overwritten in endpoint from route parameter
            Name = input.Name,
            Description = input.Description
        };
    }
}
