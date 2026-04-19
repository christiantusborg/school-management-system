namespace School.SubjectApi.Subject.V1.Update.Command;

public sealed record SubjectV1UpdateCommand : IHandleableCommand<
    SubjectV1UpdateCommand,
    SubjectV1UpdateCommandValidator,
    SubjectV1UpdateCommandHandler,
    SubjectV1UpdateCommandResult>
{
    public required Guid SubjectId { get; init; }
    public required Guid MajorId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required int Ects { get; init; }
}
