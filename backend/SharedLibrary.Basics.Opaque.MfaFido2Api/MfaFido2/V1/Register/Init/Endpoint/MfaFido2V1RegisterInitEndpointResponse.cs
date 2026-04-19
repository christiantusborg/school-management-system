namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Register.Init.Endpoint;

public class MfaFido2V1RegisterInitEndpointResponse : HateoasBaseResponse
{
    public required string OptionsJson { get; init; }
}
