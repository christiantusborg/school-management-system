namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Command;

public sealed class AdminUsersV1ChangeRoleCommand
    : IHandleableCommand<AdminUsersV1ChangeRoleCommand, AdminUsersV1ChangeRoleCommandValidator,
        AdminUsersV1ChangeRoleCommandHandler, AdminUsersV1ChangeRoleCommandResult>
{
    public required string UserId { get; init; }
    public required string NewRole { get; init; }
}
