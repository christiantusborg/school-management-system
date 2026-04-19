namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Command;

public sealed class LogoutV1LogoutCommand : IHandleableCommand<
    LogoutV1LogoutCommand,
    LogoutV1LogoutCommandValidator,
    LogoutV1LogoutCommandHandler,
    LogoutV1LogoutCommandResult>
{
    public required string RawToken { get; init; }
}
