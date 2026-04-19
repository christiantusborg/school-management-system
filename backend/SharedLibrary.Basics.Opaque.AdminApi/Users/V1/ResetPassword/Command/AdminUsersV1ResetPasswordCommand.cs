namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ResetPassword.Command;

public sealed class AdminUsersV1ResetPasswordCommand
    : IHandleableCommand<AdminUsersV1ResetPasswordCommand, AdminUsersV1ResetPasswordCommandValidator,
        AdminUsersV1ResetPasswordCommandHandler, AdminUsersV1ResetPasswordCommandResult>
{
    public required string UserId { get; init; }
}
