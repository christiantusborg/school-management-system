using System.Text.Json.Nodes;

namespace School.PathwayApi.EducationLevel.V1.Options.Endpoint;

public sealed class EducationLevelV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}
