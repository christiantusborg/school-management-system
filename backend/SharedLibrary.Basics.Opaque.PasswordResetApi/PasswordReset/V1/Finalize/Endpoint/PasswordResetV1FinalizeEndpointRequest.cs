namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Endpoint;

public sealed class PasswordResetV1FinalizeEndpointRequest
{
    public required string ResetId { get; init; }
    public required string ClientPublicKey { get; init; }
}
