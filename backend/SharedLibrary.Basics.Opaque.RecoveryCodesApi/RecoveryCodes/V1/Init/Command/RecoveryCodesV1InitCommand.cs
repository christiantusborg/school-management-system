namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.Init.Command;

public sealed record RecoveryCodesV1InitCommand : IHandleableCommand<
    RecoveryCodesV1InitCommand,
    RecoveryCodesV1InitCommandValidator,
    RecoveryCodesV1InitCommandHandler,
    RecoveryCodesV1InitCommandResult>
{
    public required string UserId { get; init; }
    public required string[] CodeIds { get; init; }
    public required string[] BlindedElements { get; init; }
}
