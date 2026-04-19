using System.Text.Json.Nodes;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Options.Endpoint;

public sealed class FinalProjectStatusV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}
