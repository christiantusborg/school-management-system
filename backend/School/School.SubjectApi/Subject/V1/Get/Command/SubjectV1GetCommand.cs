namespace School.SubjectApi.Subject.V1.Get.Command;

public sealed record SubjectV1GetCommand : IHandleableCommand<
    SubjectV1GetCommand,
    SubjectV1GetCommandValidator,
    SubjectV1GetCommandHandler,
    SubjectV1GetCommandResult>
{
    public required Guid SubjectId { get; init; }
}
