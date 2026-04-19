namespace SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Command;

public sealed class MeV1GetCommand
    : IHandleableCommand<MeV1GetCommand, MeV1GetCommandValidator, MeV1GetCommandHandler, MeV1GetCommandResult>
{
    public required string UserId { get; init; }
}
