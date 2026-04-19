namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Command;

public sealed record EnrollmentStatusV1GetCommand : IHandleableCommand<
    EnrollmentStatusV1GetCommand,
    EnrollmentStatusV1GetCommandValidator,
    EnrollmentStatusV1GetCommandHandler,
    EnrollmentStatusV1GetCommandResult>
{
    public required int EnrollmentStatusId { get; init; }
}
