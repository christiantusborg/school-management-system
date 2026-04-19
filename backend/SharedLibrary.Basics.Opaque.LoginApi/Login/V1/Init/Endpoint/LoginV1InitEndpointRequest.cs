namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Endpoint;

public class LoginV1InitEndpointRequest
{
    public required string Username { get; init; }
    public required string BlindedElement { get; init; }
    public string? DeviceInfo { get; init; }
}
