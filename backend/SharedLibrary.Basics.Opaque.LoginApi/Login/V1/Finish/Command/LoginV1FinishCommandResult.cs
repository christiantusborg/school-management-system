using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;

public sealed class LoginV1FinishCommandResult : ILoginV1FinishCommandResultQueue
{
    public string? Token { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? MfaPendingId { get; init; }
    public string[]? AvailableMethods { get; init; }
}
