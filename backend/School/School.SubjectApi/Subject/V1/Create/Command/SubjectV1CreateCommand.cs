namespace School.SubjectApi.Subject.V1.Create.Command;

public sealed record SubjectV1CreateCommand : IHandleableCommand<
    SubjectV1CreateCommand,
    SubjectV1CreateCommandValidator,
    SubjectV1CreateCommandHandler,
    SubjectV1CreateCommandResult>
{
    public required Guid MajorId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int Ects { get; init; }
}
