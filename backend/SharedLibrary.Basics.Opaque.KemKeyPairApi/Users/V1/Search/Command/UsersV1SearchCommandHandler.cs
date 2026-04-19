namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.Users.V1.Search.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class UsersV1SearchCommandHandler(IApplicationUserRepository repository, IUserProfileRepository profileRepository)
    : ICommandHandler<UsersV1SearchCommand, UsersV1SearchCommandResult, ApplicationUser, IApplicationUserRepository>
{
    public async Task<SuccessOrFailure<UsersV1SearchCommandResult>> HandleAsync(
        UsersV1SearchCommand command, CancellationToken cancellationToken)
    {
        var q = command.Query.Trim().ToLower();
        var spec = new Specification<ApplicationUser>()
            .AddWhere(x => x.UserName != null && x.UserName.ToLower().Contains(q));

        var users = (await repository.SearchAsync(spec, cancellationToken).ConfigureAwait(false))
            .Take(20).ToList();

        var userIds = users.Select(u => u.Id).ToList();
        var profileSpec = new Specification<UserProfile>().AddWhere(x => userIds.Contains(x.UserId));
        var profiles = (await profileRepository.SearchAsync(profileSpec, cancellationToken).ConfigureAwait(false))
            .ToDictionary(p => p.UserId);

        return new SuccessOrFailure<UsersV1SearchCommandResult>(new UsersV1SearchCommandResult
        {
            Items = users.Select(u =>
            {
                profiles.TryGetValue(u.Id, out var profile);
                return new UsersV1SearchCommandResultItem
                {
                    UserId = u.Id,
                    Username = u.UserName ?? u.Id,
                    FirstName = profile?.FirstName,
                    LastName = profile?.LastName,
                    Email = u.Email
                };
            }).ToList()
        });
    }
}
