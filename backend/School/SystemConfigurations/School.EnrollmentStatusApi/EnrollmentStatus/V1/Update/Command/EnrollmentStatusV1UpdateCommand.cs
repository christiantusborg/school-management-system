namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Command;

public sealed record EnrollmentStatusV1UpdateCommand : IHandleableCommand<
    EnrollmentStatusV1UpdateCommand,
    EnrollmentStatusV1UpdateCommandValidator,
    EnrollmentStatusV1UpdateCommandHandler,
    EnrollmentStatusV1UpdateCommandResult>
{
    public required int EnrollmentStatusId { get; init; }
    public required string Name { get; init; }
    public bool AllowSetByPartner { get; init; }
}
