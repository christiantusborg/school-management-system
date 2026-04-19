namespace School.MajorApi.Major.V1.SoftDelete.Command;

public sealed record MajorV1SoftDeleteCommand : IHandleableCommand<
    MajorV1SoftDeleteCommand,
    MajorV1SoftDeleteCommandValidator,
    MajorV1SoftDeleteCommandHandler,
    MajorV1SoftDeleteCommandResult>
{
    public required Guid MajorId { get; init; }
}
