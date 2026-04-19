namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Endpoint;

public class ChangePasswordV1InitEndpointRequest
{
    public required string OldBlindedElement { get; init; }
    public required string BlindedElement { get; init; }
}
