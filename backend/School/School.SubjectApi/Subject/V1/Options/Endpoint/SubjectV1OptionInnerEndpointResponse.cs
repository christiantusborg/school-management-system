using System.Text.Json.Nodes;

namespace School.SubjectApi.Subject.V1.Options.Endpoint;

public sealed class SubjectV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}
