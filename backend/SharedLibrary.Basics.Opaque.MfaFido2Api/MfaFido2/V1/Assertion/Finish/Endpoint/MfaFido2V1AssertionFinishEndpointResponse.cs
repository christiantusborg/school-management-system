namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Finish.Endpoint;

public class MfaFido2V1AssertionFinishEndpointResponse : HateoasBaseResponse
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
