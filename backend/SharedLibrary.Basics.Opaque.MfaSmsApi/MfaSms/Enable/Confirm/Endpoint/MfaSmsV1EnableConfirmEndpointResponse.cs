namespace SharedLibrary.Basics.Opaque.MfaSmsApi.MfaSms.V1.Enable.Confirm.Endpoint;

public class MfaSmsV1EnableConfirmEndpointResponse : HateoasBaseResponse
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
