using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Command;

public sealed class RegisterFinalizeV1CreateCommandResult : IRegisterFinalizeV1CreateCommandResultQueue
{
    public required string UserId { get; init; }
    public required string Token { get; init; }
}
