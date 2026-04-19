namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.ChangeRole.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1ChangeRoleCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<AdminUsersV1ChangeRoleCommand, AdminUsersV1ChangeRoleCommandResult,
        ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<AdminUsersV1ChangeRoleCommandResult>> HandleAsync(
        AdminUsersV1ChangeRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return SuccessOrFailureHelper<AdminUsersV1ChangeRoleCommandResult>.EntityNotFound(
                typeof(AdminUsersV1ChangeRoleCommand));

        var currentRoles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, currentRoles);
        await userManager.AddToRoleAsync(user, command.NewRole);

        return new SuccessOrFailure<AdminUsersV1ChangeRoleCommandResult>(new AdminUsersV1ChangeRoleCommandResult());
    }
}
