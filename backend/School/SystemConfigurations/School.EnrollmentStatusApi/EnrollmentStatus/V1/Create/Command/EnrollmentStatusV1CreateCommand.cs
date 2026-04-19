namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Command;

public sealed record EnrollmentStatusV1CreateCommand : IHandleableCommand<
    EnrollmentStatusV1CreateCommand,
    EnrollmentStatusV1CreateCommandValidator,
    EnrollmentStatusV1CreateCommandHandler,
    EnrollmentStatusV1CreateCommandResult>
{
    public required string Name { get; init; }
    public bool AllowSetByPartner { get; init; }
}
