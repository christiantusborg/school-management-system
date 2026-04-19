namespace School.ProgrammeApi.Programme.V1.Get.Command;

public sealed record ProgrammeV1GetCommand : IHandleableCommand<
    ProgrammeV1GetCommand,
    ProgrammeV1GetCommandValidator,
    ProgrammeV1GetCommandHandler,
    ProgrammeV1GetCommandResult>
{
    public required Guid ProgrammeId { get; init; }
}
