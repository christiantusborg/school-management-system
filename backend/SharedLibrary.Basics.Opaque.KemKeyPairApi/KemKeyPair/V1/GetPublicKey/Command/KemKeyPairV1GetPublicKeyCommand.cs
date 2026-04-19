namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Command;

public sealed class KemKeyPairV1GetPublicKeyCommand
    : IHandleableCommand<KemKeyPairV1GetPublicKeyCommand, KemKeyPairV1GetPublicKeyCommandValidator, KemKeyPairV1GetPublicKeyCommandHandler, KemKeyPairV1GetPublicKeyCommandResult>
{
    public required string UserId { get; init; }
}
