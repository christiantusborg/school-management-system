using System.Text.Json.Nodes;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Options.Endpoint;

public sealed class TuitionFeeStatusV1OptionInnerEndpointResponse
{
    public JsonNode? Request { get; init; }
    public JsonNode? Result { get; init; }
}
