namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Endpoint;

public class LoginV1FinishEndpointRequest
{
    public required string LoginId { get; init; }
    public required string Signature { get; init; }
}
