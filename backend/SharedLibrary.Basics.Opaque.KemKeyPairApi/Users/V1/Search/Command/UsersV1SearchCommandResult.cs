namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Command;

public sealed class UsersV1SearchCommandResult : IUsersV1SearchCommandResultQueue
{
    public required IReadOnlyList<UsersV1SearchCommandResultItem> Items { get; init; }
}

public sealed class UsersV1SearchCommandResultItem
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
}
