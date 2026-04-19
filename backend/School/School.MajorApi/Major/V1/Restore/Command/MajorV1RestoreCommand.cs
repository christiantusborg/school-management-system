namespace School.MajorApi.Major.V1.Restore.Command;

public sealed record MajorV1RestoreCommand : IHandleableCommand<
    MajorV1RestoreCommand,
    MajorV1RestoreCommandValidator,
    MajorV1RestoreCommandHandler,
    MajorV1RestoreCommandResult>
{
    public required Guid MajorId { get; init; }
}
