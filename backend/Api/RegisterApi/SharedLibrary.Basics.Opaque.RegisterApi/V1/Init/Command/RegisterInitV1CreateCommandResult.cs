using SharedLibrary.Basics.Opaque.MessageQueues;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;

public sealed class RegisterInitV1CreateCommandResult : IRegisterInitV1CreateCommandResultQueue
{
    public required Guid RegistrationId { get; init; }
    public required byte[] EvaluatedElement { get; init; }
}