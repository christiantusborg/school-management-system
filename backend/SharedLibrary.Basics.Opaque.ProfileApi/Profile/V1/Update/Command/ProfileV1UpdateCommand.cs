namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Command;

public sealed record ProfileV1UpdateCommand : IHandleableCommand<
    ProfileV1UpdateCommand,
    ProfileV1UpdateCommandValidator,
    ProfileV1UpdateCommandHandler,
    ProfileV1UpdateCommandResult>
{
    public required string UserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
    public DateTime? DateOfBirth { get; init; }
}
