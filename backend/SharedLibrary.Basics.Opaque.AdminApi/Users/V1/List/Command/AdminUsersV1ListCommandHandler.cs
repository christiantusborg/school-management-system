namespace SharedLibrary.Basics.Opaque.AdminApi.Users.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminUsersV1ListCommandHandler(
    IApplicationUserRepository userRepository,
    UserManager<ApplicationUser> userManager)
    : ICommandHandler<AdminUsersV1ListCommand, CommandSearchResult<AdminUsersV1ListCommandResultItem>,
        ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<AdminUsersV1ListCommandResultItem>>> HandleAsync(
        AdminUsersV1ListCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<ApplicationUser>();
        if (!string.IsNullOrWhiteSpace(command.Search))
            spec.AddWhere(x => (x.UserName != null && x.UserName.Contains(command.Search))
                            || (x.Email != null && x.Email.Contains(command.Search)));

        var all = await userRepository.SearchAsync(spec, cancellationToken).ConfigureAwait(false);
        var total = all.Count;

        var pageUsers = all
            .OrderBy(u => u.UserName)
            .Skip((command.Page - 1) * command.PageSize)
            .Take(command.PageSize)
            .ToList();

        var items = new List<AdminUsersV1ListCommandResultItem>();
        foreach (var u in pageUsers)
        {
            var roles = await userManager.GetRolesAsync(u);
            items.Add(new AdminUsersV1ListCommandResultItem
            {
                UserId = u.Id,
                Username = u.UserName!,
                Email = u.Email,
                IsEnabled = u.IsEnabled,
                Roles = roles.ToArray(),
                CreatedAt = u.CreatedAt
            });
        }

        return new SuccessOrFailure<CommandSearchResult<AdminUsersV1ListCommandResultItem>>(
            new CommandSearchResult<AdminUsersV1ListCommandResultItem> { Items = items, Total = total });
    }
}
