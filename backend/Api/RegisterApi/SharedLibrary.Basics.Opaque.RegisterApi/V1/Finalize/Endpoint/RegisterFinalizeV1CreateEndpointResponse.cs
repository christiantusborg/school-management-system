namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Endpoint;

public class RegisterFinalizeV1CreateEndpointResponse : HateoasBaseResponse
{
    public required string Token { get; init; }
}
