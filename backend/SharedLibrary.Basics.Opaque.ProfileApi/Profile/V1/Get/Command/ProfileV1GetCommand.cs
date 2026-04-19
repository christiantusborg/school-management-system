namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Command;

public sealed class ProfileV1GetCommand : IHandleableCommand<
    ProfileV1GetCommand,
    ProfileV1GetCommandValidator,
    ProfileV1GetCommandHandler,
    ProfileV1GetCommandResult>
{
    public required string UserId { get; init; }
}
