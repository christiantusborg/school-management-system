namespace School.ProgrammeApi.Programme.V1.SoftDelete.Command;

public sealed record ProgrammeV1SoftDeleteCommand : IHandleableCommand<
    ProgrammeV1SoftDeleteCommand,
    ProgrammeV1SoftDeleteCommandValidator,
    ProgrammeV1SoftDeleteCommandHandler,
    ProgrammeV1SoftDeleteCommandResult>
{
    public required Guid ProgrammeId { get; init; }
}
