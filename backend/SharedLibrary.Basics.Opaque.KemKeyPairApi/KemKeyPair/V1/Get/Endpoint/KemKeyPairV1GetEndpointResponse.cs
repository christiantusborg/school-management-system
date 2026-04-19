namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Endpoint;

public sealed class KemKeyPairV1GetEndpointResponse : HateoasBaseResponse
{
    public required string PublicKey { get; init; }
    public required string EncryptedPrivateKey { get; init; }
    public required string Nonce { get; init; }
}
