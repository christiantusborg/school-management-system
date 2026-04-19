namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Command;

public sealed class AdminUsersV1ResetPasswordCommandResult : IAdminUsersV1ResetPasswordCommandResultQueue
{
    public required string ResetToken { get; init; }
}
