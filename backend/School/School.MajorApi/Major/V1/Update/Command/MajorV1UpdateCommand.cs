namespace School.MajorApi.Major.V1.Update.Command;

public sealed record MajorV1UpdateCommand : IHandleableCommand<
    MajorV1UpdateCommand,
    MajorV1UpdateCommandValidator,
    MajorV1UpdateCommandHandler,
    MajorV1UpdateCommandResult>
{
    public required Guid MajorId { get; init; }
    public required Guid ProgrammeId { get; init; }
    public required string Name { get; init; }
}
