namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Endpoint;

public class RecoveryCodesV1FinalizeEndpointResponse : HateoasBaseResponse
{
    public required Guid BatchId { get; init; }
}
