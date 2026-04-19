namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Command;

public sealed class KemKeyPairV1GetCommand
    : IHandleableCommand<KemKeyPairV1GetCommand, KemKeyPairV1GetCommandValidator, KemKeyPairV1GetCommandHandler, KemKeyPairV1GetCommandResult>
{
    public required string UserId { get; init; }
}
