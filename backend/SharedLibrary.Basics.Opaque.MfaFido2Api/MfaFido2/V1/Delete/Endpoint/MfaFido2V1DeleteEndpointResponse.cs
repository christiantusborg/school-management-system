namespace SharedLibrary.Basics.Opaque.MfaFido2Api.MfaFido2.V1.Delete.Endpoint;

public class MfaFido2V1DeleteEndpointResponse : HateoasBaseResponse
{
    public required Guid Fido2CredentialId { get; init; }
}
