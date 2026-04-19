using Odin.Api.Base.Authentication;

namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.LogoutEverywhere.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class LogoutV1LogoutEverywhereCommandHandler(SessionTokenService sessionTokenService)
    : ICommandHandler<LogoutV1LogoutEverywhereCommand, LogoutV1LogoutEverywhereCommandResult,
        SessionToken, ISessionTokenRepository>
{
    public async Task<SuccessOrFailure<LogoutV1LogoutEverywhereCommandResult>> HandleAsync(
        LogoutV1LogoutEverywhereCommand command, CancellationToken cancellationToken)
    {
        await sessionTokenService.RevokeAllUserTokensAsync(command.UserId, cancellationToken);
        return new SuccessOrFailure<LogoutV1LogoutEverywhereCommandResult>(new LogoutV1LogoutEverywhereCommandResult());
    }
}
