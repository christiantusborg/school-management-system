namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Finish.Command;

public sealed class LoginV1FinishCommand : IHandleableCommand<
    LoginV1FinishCommand,
    LoginV1FinishCommandValidator,
    LoginV1FinishCommandHandler,
    LoginV1FinishCommandResult>
{
    public required string LoginId { get; init; }
    public required string Signature { get; init; }
}
