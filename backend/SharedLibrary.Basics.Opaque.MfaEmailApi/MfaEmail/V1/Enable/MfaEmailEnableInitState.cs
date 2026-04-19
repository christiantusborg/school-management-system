namespace SharedLibrary.Basics.Opaque.MfaEmailApi.MfaEmail.V1.Enable;

public class MfaEmailEnableInitState
{
    public required string UserId { get; init; }
    public required string Code { get; init; }
}
