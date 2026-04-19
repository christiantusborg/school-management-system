namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Endpoint;

public class MfaEmailV1EnableConfirmEndpointRequest
{
    public required Guid SessionId { get; init; }
    public required string Code { get; init; }
}
