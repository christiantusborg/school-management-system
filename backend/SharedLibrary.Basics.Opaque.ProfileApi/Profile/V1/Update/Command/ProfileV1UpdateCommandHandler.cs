using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProfileV1UpdateCommandHandler(IUserProfileRepository repository)
    : ICommandHandler<ProfileV1UpdateCommand, ProfileV1UpdateCommandResult, UserProfile, IUserProfileRepository>
{
    public async Task<SuccessOrFailure<ProfileV1UpdateCommandResult>> HandleAsync(
        ProfileV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserProfile>()
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<ProfileV1UpdateCommandResult>.EntityNotFound(typeof(ProfileV1UpdateCommand));

        existing.FirstName = command.FirstName;
        existing.LastName = command.LastName;
        existing.AvatarUrl = command.AvatarUrl;
        existing.Bio = command.Bio;
        existing.DateOfBirth = command.DateOfBirth;

        return new SuccessOrFailure<ProfileV1UpdateCommandResult>(
            new ProfileV1UpdateCommandResult { UserProfileId = existing.UserProfileId });
    }
}
