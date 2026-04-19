namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Command;

public sealed class ProfileV1GetCommandResult : IProfileV1GetCommandResultQueue
{
    public required Guid UserProfileId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public DateTime? DateOfBirth { get; init; }
}
