namespace SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Command;

public sealed class MfaStatusV1GetCommand
    : IHandleableCommand<MfaStatusV1GetCommand, MfaStatusV1GetCommandValidator, MfaStatusV1GetCommandHandler, MfaStatusV1GetCommandResult>
{
    public required string UserId { get; init; }
}
