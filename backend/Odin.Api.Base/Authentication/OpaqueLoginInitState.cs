namespace Odin.Api.Base.Authentication;

public class OpaqueLoginInitState
{
    public required string UserId { get; init; }
    public required byte[] Challenge { get; init; }
    public required byte[] ClientPublicKey { get; init; }
    public required string? DeviceInfo { get; init; }
    public Guid? OpaqueRecoveryCodeId { get; init; }
}
