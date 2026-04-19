using System.Text.Json.Nodes;

namespace School.PathwayApi.Pathway.V1.Options.Endpoint;

public sealed class PathwayV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}
