namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Endpoint;

public class RecoveryCodesV1InitEndpointResponse : HateoasBaseResponse
{
    public required Guid BatchId { get; init; }
    public required string[] EvaluatedElements { get; init; }
}
