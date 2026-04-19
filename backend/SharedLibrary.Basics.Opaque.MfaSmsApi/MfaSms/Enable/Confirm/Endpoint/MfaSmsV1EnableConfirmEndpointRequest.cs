namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Endpoint;

public class MfaSmsV1EnableConfirmEndpointRequest
{
    public required Guid SessionId { get; init; }
    public required string Code { get; init; }
}
