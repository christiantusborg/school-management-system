namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Command;

public sealed class AdminUsersV1EnableCommand
    : IHandleableCommand<AdminUsersV1EnableCommand, AdminUsersV1EnableCommandValidator,
        AdminUsersV1EnableCommandHandler, AdminUsersV1EnableCommandResult>
{
    public required string UserId { get; init; }
}
