namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Endpoint;

public class RecoveryCodesV1InitEndpointRequest
{
    public required string[] CodeIds { get; init; }
    public required string[] BlindedElements { get; init; }
}
