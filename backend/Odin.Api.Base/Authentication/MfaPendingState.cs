namespace Odin.Api.Base.Authentication;

public class MfaPendingState
{
    public required string UserId { get; init; }
    public required string? DeviceInfo { get; init; }
    public required string[] AvailableMethods { get; init; }
}
