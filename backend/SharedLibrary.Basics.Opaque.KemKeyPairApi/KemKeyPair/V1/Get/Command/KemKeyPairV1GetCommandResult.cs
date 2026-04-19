namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Command;

public sealed class KemKeyPairV1GetCommandResult : IKemKeyPairV1GetCommandResultQueue
{
    public required string PublicKey { get; init; }
    public required string EncryptedPrivateKey { get; init; }
    public required string Nonce { get; init; }
}
