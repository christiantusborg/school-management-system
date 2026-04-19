namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Assertion.Init.Endpoint;

public class MfaFido2V1AssertionInitEndpointResponse : HateoasBaseResponse
{
    public required string OptionsJson { get; init; }
}
