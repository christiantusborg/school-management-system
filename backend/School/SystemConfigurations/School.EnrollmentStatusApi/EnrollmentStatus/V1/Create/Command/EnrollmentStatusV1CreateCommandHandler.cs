namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1CreateCommandHandler(IEnrollmentStatusRepository repository)
    : ICommandHandler<EnrollmentStatusV1CreateCommand, EnrollmentStatusV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus, IEnrollmentStatusRepository>
{
    public async Task<SuccessOrFailure<EnrollmentStatusV1CreateCommandResult>> HandleAsync(
        EnrollmentStatusV1CreateCommand command, CancellationToken cancellationToken)
    {
        var entity = new SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus
        {
            Name = command.Name,
            AllowSetByPartner = command.AllowSetByPartner,
        };
        repository.Add(entity);
        return new SuccessOrFailure<EnrollmentStatusV1CreateCommandResult>(
            new EnrollmentStatusV1CreateCommandResult { EnrollmentStatusId = entity.EnrollmentStatusId });
    }
}
