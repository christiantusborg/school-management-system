namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Endpoint;

public class LoginV1InitEndpointResponse : HateoasBaseResponse
{
    public required string LoginId { get; init; }
    public required string EvaluatedElement { get; init; }
    public required string Challenge { get; init; }
}
