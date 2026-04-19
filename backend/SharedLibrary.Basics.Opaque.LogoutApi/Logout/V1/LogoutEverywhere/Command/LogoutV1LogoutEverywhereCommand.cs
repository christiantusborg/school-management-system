namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Command;

public sealed class LogoutV1LogoutEverywhereCommand : IHandleableCommand<
    LogoutV1LogoutEverywhereCommand,
    LogoutV1LogoutEverywhereCommandValidator,
    LogoutV1LogoutEverywhereCommandHandler,
    LogoutV1LogoutEverywhereCommandResult>
{
    public required string UserId { get; init; }
}
