namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Endpoint;

public sealed class RecoveryLoginV1InitEndpointResponse : HateoasBaseResponse
{
    public required string LoginId { get; init; }
    public required string EvaluatedElement { get; init; }
    public required string Challenge { get; init; }
    public required string EncryptedPrivateKey { get; init; }
    public required string Nonce { get; init; }
}
