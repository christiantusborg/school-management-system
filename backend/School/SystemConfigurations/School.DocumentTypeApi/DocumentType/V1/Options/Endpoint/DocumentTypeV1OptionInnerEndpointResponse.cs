using System.Text.Json.Nodes;

namespace School.DocumentTypeApi.DocumentType.V1.Options.Endpoint;

public sealed class DocumentTypeV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}