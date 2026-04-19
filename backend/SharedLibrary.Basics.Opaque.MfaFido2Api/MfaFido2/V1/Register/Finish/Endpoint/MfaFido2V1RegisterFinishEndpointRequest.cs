using System.Text.Json;

namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Finish.Endpoint;

public class MfaFido2V1RegisterFinishEndpointRequest
{
    public required string Label { get; init; }
    public required JsonElement AttestationResponse { get; init; }
}
