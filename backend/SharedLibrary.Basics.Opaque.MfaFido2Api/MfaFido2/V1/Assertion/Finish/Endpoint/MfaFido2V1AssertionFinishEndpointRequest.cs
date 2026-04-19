using System.Text.Json;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Endpoint;

public class MfaFido2V1AssertionFinishEndpointRequest
{
    public required string PendingId { get; init; }
    public required JsonElement AssertionResponse { get; init; }
}
