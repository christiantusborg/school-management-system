namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Endpoint;

public sealed class UsersV1SearchEndpointResponse : HateoasBaseResponse
{
    public required IReadOnlyList<UsersV1SearchEndpointResponseItem> Items { get; init; }
}

public sealed class UsersV1SearchEndpointResponseItem
{
    public required string UserId { get; init; }
    public required string Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
}
