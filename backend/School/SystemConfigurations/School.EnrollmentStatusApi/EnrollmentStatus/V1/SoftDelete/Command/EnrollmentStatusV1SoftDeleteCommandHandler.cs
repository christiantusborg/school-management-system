namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1SoftDeleteCommandHandler(IEnrollmentStatusRepository repository)
    : ICommandHandler<EnrollmentStatusV1SoftDeleteCommand, EnrollmentStatusV1SoftDeleteCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus, IEnrollmentStatusRepository>
{
    public async Task<SuccessOrFailure<EnrollmentStatusV1SoftDeleteCommandResult>> HandleAsync(
        EnrollmentStatusV1SoftDeleteCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus>()
            .AddWhere(x => x.EnrollmentStatusId == command.EnrollmentStatusId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<EnrollmentStatusV1SoftDeleteCommandResult>.EntityNotFound(typeof(EnrollmentStatusV1SoftDeleteCommand));

        entity.DeletedAt = DateTime.UtcNow;

        return new SuccessOrFailure<EnrollmentStatusV1SoftDeleteCommandResult>(
            new EnrollmentStatusV1SoftDeleteCommandResult { EnrollmentStatusId = entity.EnrollmentStatusId });
    }
}
