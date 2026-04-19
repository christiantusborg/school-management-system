namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1;

public class RecoveryCodesBatchInitState
{
    public required string UserId { get; init; }
    public required string[] CodeIds { get; init; }
    public required byte[][] OprfSeeds { get; init; }
}
