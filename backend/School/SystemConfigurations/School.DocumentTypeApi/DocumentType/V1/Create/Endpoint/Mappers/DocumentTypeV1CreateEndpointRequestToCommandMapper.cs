using School.DocumentTypeApi.DocumentType.V1.Create.Command;
using School.DocumentTypeApi.DocumentType.V1.Create.Endpoint;

namespace School.DocumentTypeApi.DocumentType.V1.Create.Endpoint.Mappers;

public sealed class DocumentTypeV1CreateEndpointRequestToCommandMapper
    : IMapper<DocumentTypeV1CreateEndpointRequest, DocumentTypeV1CreateCommand>
{
    public DocumentTypeV1CreateCommand MapFrom(DocumentTypeV1CreateEndpointRequest input)
    {
        Debug.Assert(input != null, nameof(input) + " != null");
        return new DocumentTypeV1CreateCommand
        {
            Name = input.Name,
            Description = input.Description
        };
    }
}
