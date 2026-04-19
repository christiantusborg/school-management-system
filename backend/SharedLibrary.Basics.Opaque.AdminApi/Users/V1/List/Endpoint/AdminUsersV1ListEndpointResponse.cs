namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Endpoint;

public sealed class AdminUsersV1ListEndpointResponse : HateoasBaseResponse
{
    public required IReadOnlyList<AdminUsersV1ListEndpointResponseItem> Items { get; init; }
    public required int Total { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

public sealed class AdminUsersV1ListEndpointResponseItem
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? Email { get; init; }
    public required bool IsEnabled { get; init; }
    public required string[] Roles { get; init; }
    public required DateTime CreatedAt { get; init; }
}
