namespace School.MajorApi.Major.V1.Get.Command;

public sealed record MajorV1GetCommand : IHandleableCommand<
    MajorV1GetCommand,
    MajorV1GetCommandValidator,
    MajorV1GetCommandHandler,
    MajorV1GetCommandResult>
{
    public required Guid MajorId { get; init; }
}
