namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1PermanentDeleteCommandHandler(IEnrollmentStatusRepository repository)
    : ICommandHandler<EnrollmentStatusV1PermanentDeleteCommand, EnrollmentStatusV1PermanentDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus, IEnrollmentStatusRepository>
{
    public async Task<SuccessOrFailure<EnrollmentStatusV1PermanentDeleteCommandResult>> HandleAsync(
        EnrollmentStatusV1PermanentDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus>()
            .AddWhere(x => x.EnrollmentStatusId == command.EnrollmentStatusId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<EnrollmentStatusV1PermanentDeleteCommandResult>.EntityNotFound(typeof(EnrollmentStatusV1PermanentDeleteCommand));

        var id = entity.EnrollmentStatusId;
        repository.Remove(entity);

        return new SuccessOrFailure<EnrollmentStatusV1PermanentDeleteCommandResult>(
            new EnrollmentStatusV1PermanentDeleteCommandResult { EnrollmentStatusId = id });
    }
}
