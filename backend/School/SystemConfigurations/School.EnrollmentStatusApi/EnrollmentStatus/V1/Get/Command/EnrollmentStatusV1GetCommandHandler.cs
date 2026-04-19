namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class EnrollmentStatusV1GetCommandHandler(IEnrollmentStatusRepository repository)
    : ICommandHandler<EnrollmentStatusV1GetCommand, EnrollmentStatusV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus, IEnrollmentStatusRepository>
{
    public async Task<SuccessOrFailure<EnrollmentStatusV1GetCommandResult>> HandleAsync(
        EnrollmentStatusV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.EnrollmentStatus>()
            .AddWhere(x => x.EnrollmentStatusId == command.EnrollmentStatusId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<EnrollmentStatusV1GetCommandResult>.EntityNotFound(typeof(EnrollmentStatusV1GetCommand));

        return new SuccessOrFailure<EnrollmentStatusV1GetCommandResult>(new EnrollmentStatusV1GetCommandResult
        {
            EnrollmentStatusId = entity.EnrollmentStatusId,
            Name = entity.Name,
            AllowSetByPartner = entity.AllowSetByPartner,
            DeletedAt = entity.DeletedAt
        });
    }
}
