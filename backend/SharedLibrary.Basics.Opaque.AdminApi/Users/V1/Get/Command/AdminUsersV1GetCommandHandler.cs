namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1GetCommandHandler(
    UserManager<ApplicationUser> userManager,
    IUserProfileRepository profileRepository)
    : ICommandHandler<AdminUsersV1GetCommand, AdminUsersV1GetCommandResult,
        ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<AdminUsersV1GetCommandResult>> HandleAsync(
        AdminUsersV1GetCommand command, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(command.UserId);
        if (user is null)
            return SuccessOrFailureHelper<AdminUsersV1GetCommandResult>.EntityNotFound(
                typeof(AdminUsersV1GetCommand));

        var roles = await userManager.GetRolesAsync(user);

        var profileSpec = new Specification<UserProfile>()
            .AddWhere(x => x.UserId == command.UserId);
        var profile = await profileRepository.GetAsync(profileSpec, cancellationToken).ConfigureAwait(false);

        return new SuccessOrFailure<AdminUsersV1GetCommandResult>(new AdminUsersV1GetCommandResult
        {
            UserId = user.Id,
            Username = user.UserName!,
            Email = user.Email,
            IsEnabled = user.IsEnabled,
            Roles = roles.ToArray(),
            CreatedAt = user.CreatedAt,
            FirstName = profile?.FirstName,
            LastName = profile?.LastName,
            AvatarUrl = profile?.AvatarUrl,
            Bio = profile?.Bio,
            DateOfBirth = profile?.DateOfBirth
        });
    }
}
