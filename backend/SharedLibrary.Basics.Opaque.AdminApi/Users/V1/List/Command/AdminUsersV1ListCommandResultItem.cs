namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Command;

public sealed class AdminUsersV1ListCommandResultItem : IAdminUsersV1ListCommandResultQueue
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public required bool IsEnabled { get; init; }
    public required string[] Roles { get; init; }
    public required DateTime CreatedAt { get; init; }
}
