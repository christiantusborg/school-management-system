namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Finalize.Endpoint;

public class RegisterFinalizeV1CreateEndpointRequest
{
    public required Guid RegistrationId { get; init; }
    public required string ClientPublicKey { get; init; }
}
