namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Command;

public sealed class ProfileV1UpdateCommandResult : IProfileV1UpdateCommandResultQueue
{
    public required Guid UserProfileId { get; init; }
}
