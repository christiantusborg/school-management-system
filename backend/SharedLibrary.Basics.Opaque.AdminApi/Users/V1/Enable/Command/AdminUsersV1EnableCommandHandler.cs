namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Enable.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1EnableCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<AdminUsersV1EnableCommand, AdminUsersV1EnableCommandResult,
        ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<AdminUsersV1EnableCommandResult>> HandleAsync(
        AdminUsersV1EnableCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return SuccessOrFailureHelper<AdminUsersV1EnableCommandResult>.EntityNotFound(
                typeof(AdminUsersV1EnableCommand));

        user.IsEnabled = true;
        await userManager.UpdateAsync(user);

        return new SuccessOrFailure<AdminUsersV1EnableCommandResult>(new AdminUsersV1EnableCommandResult());
    }
}
