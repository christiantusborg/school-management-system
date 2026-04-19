namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Finalize.Endpoint;

public sealed class PasswordResetV1FinalizeEndpointResponse : HateoasBaseResponse
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
