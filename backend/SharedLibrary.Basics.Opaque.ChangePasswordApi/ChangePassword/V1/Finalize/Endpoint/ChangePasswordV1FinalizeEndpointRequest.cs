namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Finalize.Endpoint;

public class ChangePasswordV1FinalizeEndpointRequest
{
    public required Guid ChangeId { get; init; }
    public required string Signature { get; init; }
    public required string ClientPublicKey { get; init; }
}
