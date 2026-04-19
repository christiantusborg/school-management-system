using System.Text.Json.Nodes;

namespace School.ProgrammeApi.Programme.V1.Options.Endpoint;

public sealed class ProgrammeV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}
