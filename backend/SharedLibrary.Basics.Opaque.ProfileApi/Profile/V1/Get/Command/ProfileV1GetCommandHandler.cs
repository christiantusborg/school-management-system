using SharedLibrary.Basics.Opaque.Domains.Repositories;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProfileV1GetCommandHandler(IUserProfileRepository repository)
    : ICommandHandler<ProfileV1GetCommand, ProfileV1GetCommandResult, UserProfile, IUserProfileRepository>
{
    public async Task<SuccessOrFailure<ProfileV1GetCommandResult>> HandleAsync(
        ProfileV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<UserProfile>()
            .AddWhere(x => x.UserId == command.UserId);

        var existing = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (existing == null)
            return SuccessOrFailureHelper<ProfileV1GetCommandResult>.EntityNotFound(typeof(ProfileV1GetCommand));

        return new SuccessOrFailure<ProfileV1GetCommandResult>(new ProfileV1GetCommandResult
        {
            UserProfileId = existing.UserProfileId,
            FirstName = existing.FirstName,
            LastName = existing.LastName,
            AvatarUrl = existing.AvatarUrl,
            Bio = existing.Bio,
            DateOfBirth = existing.DateOfBirth
        });
    }
}
