namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Endpoint;

public sealed class PasswordResetV1InitEndpointResponse : HateoasBaseResponse
{
    public required string ResetId { get; init; }
    public required string EvaluatedElement { get; init; }
}
