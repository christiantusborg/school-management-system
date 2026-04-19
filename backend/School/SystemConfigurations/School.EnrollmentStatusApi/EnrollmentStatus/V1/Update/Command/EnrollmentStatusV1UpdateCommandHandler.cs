namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1UpdateCommandHandler(IEnrollmentStatusRepository repository)
    : ICommandHandler<EnrollmentStatusV1UpdateCommand, EnrollmentStatusV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus, IEnrollmentStatusRepository>
{
    public async Task<SuccessOrFailure<EnrollmentStatusV1UpdateCommandResult>> HandleAsync(
        EnrollmentStatusV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus>()
            .AddWhere(x => x.EnrollmentStatusId == command.EnrollmentStatusId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<EnrollmentStatusV1UpdateCommandResult>.EntityNotFound(typeof(EnrollmentStatusV1UpdateCommand));

        entity.Name = command.Name;
        entity.AllowSetByPartner = command.AllowSetByPartner;

        return new SuccessOrFailure<EnrollmentStatusV1UpdateCommandResult>(
            new EnrollmentStatusV1UpdateCommandResult { EnrollmentStatusId = entity.EnrollmentStatusId });
    }
}
