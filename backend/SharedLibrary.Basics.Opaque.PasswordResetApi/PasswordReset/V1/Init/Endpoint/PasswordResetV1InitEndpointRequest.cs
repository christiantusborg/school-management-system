namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Endpoint;

public sealed class PasswordResetV1InitEndpointRequest
{
    public required string ResetToken { get; init; }
    public required string BlindedElement { get; init; }
}
