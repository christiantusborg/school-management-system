namespace SharedLibrary.Basics.Opaque.PasswordResetApi.PasswordReset.V1.Init.Command;

public sealed class PasswordResetV1InitCommand
    : IHandleableCommand<PasswordResetV1InitCommand, PasswordResetV1InitCommandValidator,
        PasswordResetV1InitCommandHandler, PasswordResetV1InitCommandResult>
{
    public required string ResetToken { get; init; }
    public required string BlindedElement { get; init; }
}
