using Odin.Api.Base.Authentication;

namespace SharedLibrary.Basics.Opaque.LogoutApi.Logout.V1.Logout.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class LogoutV1LogoutCommandHandler(SessionTokenService sessionTokenService)
    : ICommandHandler<LogoutV1LogoutCommand, LogoutV1LogoutCommandResult,
        SessionToken, ISessionTokenRepository>
{
    public async Task<SuccessOrFailure<LogoutV1LogoutCommandResult>> HandleAsync(
        LogoutV1LogoutCommand command, CancellationToken cancellationToken)
    {
        await sessionTokenService.RevokeTokenAsync(command.RawToken, cancellationToken);
        return new SuccessOrFailure<LogoutV1LogoutCommandResult>(new LogoutV1LogoutCommandResult());
    }
}
