namespace SharedLibrary.Basics.Opaque.RecoveryLoginApi.RecoveryLogin.V1.Init.Command;

public sealed class RecoveryLoginV1InitCommand
    : IHandleableCommand<RecoveryLoginV1InitCommand, RecoveryLoginV1InitCommandValidator,
        RecoveryLoginV1InitCommandHandler, RecoveryLoginV1InitCommandResult>
{
    public required string Username { get; init; }
    public required string CodeId { get; init; }
    public required string BlindedElement { get; init; }
    public string? DeviceInfo { get; init; }
}
