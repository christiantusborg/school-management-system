namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Disable.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1DisableCommandHandler(
    UserManager<ApplicationUser> userManager,
    SessionTokenService sessionTokenService)
    : ICommandHandler<AdminUsersV1DisableCommand, AdminUsersV1DisableCommandResult,
        ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<AdminUsersV1DisableCommandResult>> HandleAsync(
        AdminUsersV1DisableCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return SuccessOrFailureHelper<AdminUsersV1DisableCommandResult>.EntityNotFound(
                typeof(AdminUsersV1DisableCommand));

        user.IsEnabled = false;
        await userManager.UpdateAsync(user);
        await sessionTokenService.RevokeAllUserTokensAsync(command.UserId, cancellationToken);

        return new SuccessOrFailure<AdminUsersV1DisableCommandResult>(new AdminUsersV1DisableCommandResult());
    }
}
