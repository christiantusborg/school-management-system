namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1RestoreCommandHandler(IEnrollmentStatusRepository repository)
    : ICommandHandler<EnrollmentStatusV1RestoreCommand, EnrollmentStatusV1RestoreCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus, IEnrollmentStatusRepository>
{
    public async Task<SuccessOrFailure<EnrollmentStatusV1RestoreCommandResult>> HandleAsync(
        EnrollmentStatusV1RestoreCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus>()
            .AddWhere(x => x.EnrollmentStatusId == command.EnrollmentStatusId)
            .AddWhere(x => x.DeletedAt != null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<EnrollmentStatusV1RestoreCommandResult>.EntityNotFound(typeof(EnrollmentStatusV1RestoreCommand));

        entity.DeletedAt = null;

        return new SuccessOrFailure<EnrollmentStatusV1RestoreCommandResult>(
            new EnrollmentStatusV1RestoreCommandResult { EnrollmentStatusId = entity.EnrollmentStatusId });
    }
}
