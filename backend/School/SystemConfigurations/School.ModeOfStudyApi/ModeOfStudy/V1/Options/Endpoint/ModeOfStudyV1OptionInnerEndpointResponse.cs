using System.Text.Json.Nodes;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Options.Endpoint;

public sealed class ModeOfStudyV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}
