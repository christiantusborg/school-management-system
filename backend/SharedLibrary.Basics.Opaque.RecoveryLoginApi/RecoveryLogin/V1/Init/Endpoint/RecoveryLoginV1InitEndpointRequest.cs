namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Endpoint;

public sealed class RecoveryLoginV1InitEndpointRequest
{
    public required string Username { get; init; }
    public required string CodeId { get; init; }
    public required string BlindedElement { get; init; }
    public string? DeviceInfo { get; init; }
}
