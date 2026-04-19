using System.Text.Json.Nodes;

namespace School.MajorApi.Major.V1.Options.Endpoint;

public sealed class MajorV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}
