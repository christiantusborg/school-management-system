namespace SharedLibrary.Basics.Opaque.LoginApi.Login.V1.Init.Command;

public sealed class LoginV1InitCommand : IHandleableCommand<
    LoginV1InitCommand,
    LoginV1InitCommandValidator,
    LoginV1InitCommandHandler,
    LoginV1InitCommandResult>
{
    public required string Username { get; init; }
    public required string BlindedElement { get; init; }
    public string? DeviceInfo { get; init; }
}
