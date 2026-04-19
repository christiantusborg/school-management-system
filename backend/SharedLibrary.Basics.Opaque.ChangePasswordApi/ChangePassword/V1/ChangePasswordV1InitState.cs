namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1;

public class ChangePasswordV1InitState
{
    public required string UserId { get; init; }
    public required byte[] OprfSeed { get; init; }
    public required byte[] Challenge { get; init; }
    public required byte[] ExistingClientPublicKey { get; init; }
}
