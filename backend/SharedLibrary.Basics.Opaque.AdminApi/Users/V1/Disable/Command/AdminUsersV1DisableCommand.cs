namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Command;

public sealed class AdminUsersV1DisableCommand
    : IHandleableCommand<AdminUsersV1DisableCommand, AdminUsersV1DisableCommandValidator,
        AdminUsersV1DisableCommandHandler, AdminUsersV1DisableCommandResult>
{
    public required string UserId { get; init; }
}
