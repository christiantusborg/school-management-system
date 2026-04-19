using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;

public sealed class LoginV1InitCommandResult : ILoginV1InitCommandResultQueue
{
    public required string LoginId { get; init; }
    public required string EvaluatedElement { get; init; }
    public required string Challenge { get; init; }
}
