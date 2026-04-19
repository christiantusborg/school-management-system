namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.Create.Command;

public sealed class AdminInviteCodesV1CreateCommand
    : IHandleableCommand<AdminInviteCodesV1CreateCommand, AdminInviteCodesV1CreateCommandValidator,
        AdminInviteCodesV1CreateCommandHandler, AdminInviteCodesV1CreateCommandResult>
{
    public required string CreatedByUserId { get; init; }
    public required string AssignedRole { get; init; }
    public int ExpirationDays { get; init; } = 7;
}
