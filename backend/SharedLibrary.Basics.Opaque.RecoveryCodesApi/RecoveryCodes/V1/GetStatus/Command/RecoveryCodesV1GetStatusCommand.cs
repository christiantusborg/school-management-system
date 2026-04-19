namespace SharedLibrary.Basics.Opaque.RecoveryCodesApi.RecoveryCodes.V1.GetStatus.Command;

public sealed class RecoveryCodesV1GetStatusCommand : IHandleableCommand<
    RecoveryCodesV1GetStatusCommand,
    RecoveryCodesV1GetStatusCommandValidator,
    RecoveryCodesV1GetStatusCommandHandler,
    RecoveryCodesV1GetStatusCommandResult>
{
    public required string UserId { get; init; }
}
