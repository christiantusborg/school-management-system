namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Command;

public sealed class AdminUsersV1GetCommand
    : IHandleableCommand<AdminUsersV1GetCommand, AdminUsersV1GetCommandValidator,
        AdminUsersV1GetCommandHandler, AdminUsersV1GetCommandResult>
{
    public required string UserId { get; init; }
}
