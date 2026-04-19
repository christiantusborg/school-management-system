namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Command;

public sealed record EnrollmentStatusV1ListCommand : IHandleableCommand<
    EnrollmentStatusV1ListCommand,
    EnrollmentStatusV1ListCommandValidator,
    EnrollmentStatusV1ListCommandHandler,
    CommandSearchResult<EnrollmentStatusV1ListCommandResultItem>>;
