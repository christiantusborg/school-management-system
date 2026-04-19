namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Endpoint;

public sealed class AdminInviteCodesV1ListEndpointResponse : HateoasBaseResponse
{
    public required IReadOnlyList<AdminInviteCodesV1ListEndpointResponseItem> Items { get; init; }
    public required int Total { get; init; }
}

public sealed class AdminInviteCodesV1ListEndpointResponseItem
{
    public required Guid InviteCodeId { get; init; }
    public required string Code { get; init; }
    public required string AssignedRole { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public string? RedeemedByUserId { get; init; }
    public required DateTime CreatedAt { get; init; }
}
