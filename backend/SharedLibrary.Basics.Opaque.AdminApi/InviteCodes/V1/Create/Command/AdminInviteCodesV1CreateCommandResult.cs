namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Command;

public sealed class AdminInviteCodesV1CreateCommandResult : IAdminInviteCodesV1CreateCommandResultQueue
{
    public required string Code { get; init; }
    public required string AssignedRole { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
