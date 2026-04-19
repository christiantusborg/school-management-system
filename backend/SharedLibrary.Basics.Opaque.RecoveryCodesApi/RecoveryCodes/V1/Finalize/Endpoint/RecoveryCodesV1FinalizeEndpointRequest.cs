namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Finalize.Endpoint;

public class RecoveryCodesV1FinalizeEndpointRequest
{
    public required Guid BatchId { get; init; }
    public required string[] ClientPublicKeys { get; init; }
    public required string[] EncryptedPrivateKeys { get; init; }
    public required string[] Nonces { get; init; }
}
