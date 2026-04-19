namespace SharedLibrary.Basics.Opaque.AdminApi.InviteCodes.V1.List.Command;

public sealed class AdminInviteCodesV1ListCommand
    : IHandleableCommand<AdminInviteCodesV1ListCommand, AdminInviteCodesV1ListCommandValidator,
        AdminInviteCodesV1ListCommandHandler, CommandSearchResult<AdminInviteCodesV1ListCommandResultItem>>
{
}
