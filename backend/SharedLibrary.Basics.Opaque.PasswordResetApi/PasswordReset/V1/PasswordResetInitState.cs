namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1;

public class PasswordResetInitState
{
    public required string ExistingUserId { get; init; }
    public required string ExistingRole { get; init; }
    public required byte[] OprfSeed { get; init; }
    public required string ResetToken { get; init; }
}
