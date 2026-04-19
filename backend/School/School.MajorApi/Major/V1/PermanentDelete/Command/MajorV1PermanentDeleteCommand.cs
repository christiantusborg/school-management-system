namespace School.MajorApi.Major.V1.PermanentDelete.Command;

public sealed record MajorV1PermanentDeleteCommand : IHandleableCommand<
    MajorV1PermanentDeleteCommand,
    MajorV1PermanentDeleteCommandValidator,
    MajorV1PermanentDeleteCommandHandler,
    MajorV1PermanentDeleteCommandResult>
{
    public required Guid MajorId { get; init; }
}
