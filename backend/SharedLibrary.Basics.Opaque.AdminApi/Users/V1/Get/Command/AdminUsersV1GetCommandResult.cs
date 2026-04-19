namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Command;

public sealed class AdminUsersV1GetCommandResult : IAdminUsersV1GetCommandResultQueue
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public required bool IsEnabled { get; init; }
    public required string[] Roles { get; init; }
    public required DateTime CreatedAt { get; init; }
    // Profile (may be null)
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public DateTime? DateOfBirth { get; init; }
}
