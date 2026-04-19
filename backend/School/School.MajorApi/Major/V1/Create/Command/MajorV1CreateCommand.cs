namespace School.MajorApi.Major.V1.Create.Command;

public sealed record MajorV1CreateCommand : IHandleableCommand<
    MajorV1CreateCommand,
    MajorV1CreateCommandValidator,
    MajorV1CreateCommandHandler,
    MajorV1CreateCommandResult>
{
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
}
