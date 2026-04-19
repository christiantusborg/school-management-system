namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable.Confirm.Endpoint;

public class MfaEmailV1EnableConfirmEndpointResponse : HateoasBaseResponse
{
    public required Guid UserTwoFactorMethodId { get; init; }
}
