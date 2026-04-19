namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Endpoint;

public class LoginV1FinishEndpointResponse : HateoasBaseResponse
{
    public string? Token { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? MfaPendingId { get; init; }
    public string[]? AvailableMethods { get; init; }
}
