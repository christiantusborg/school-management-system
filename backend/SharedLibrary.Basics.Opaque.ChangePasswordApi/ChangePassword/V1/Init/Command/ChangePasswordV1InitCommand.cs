namespace SharedLibrary.Basics.Opaque.ChangePasswordApi.ChangePassword.V1.Init.Command;

public sealed record ChangePasswordV1InitCommand : IHandleableCommand<
    ChangePasswordV1InitCommand,
    ChangePasswordV1InitCommandValidator,
    ChangePasswordV1InitCommandHandler,
    ChangePasswordV1InitCommandResult>
{
    public required string UserId { get; init; }
    public required string OldBlindedElement { get; init; }
    public required string BlindedElement { get; init; }
}
