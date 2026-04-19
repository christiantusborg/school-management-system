namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Command;

public sealed class AdminInviteCodesV1ListCommandResultItem : IAdminInviteCodesV1ListCommandResultQueue
{
    public required Guid InviteCodeId { get; init; }
    public required string Code { get; init; }
    public required string AssignedRole { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public string? RedeemedByUserId { get; init; }
    public required DateTime CreatedAt { get; init; }
}
