namespace School.SubjectApi.Subject.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class SubjectV1GetCommandHandler(ISubjectRepository repository)
    : ICommandHandler<SubjectV1GetCommand, SubjectV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Subject, ISubjectRepository>
{
    public async Task<SuccessOrFailure<SubjectV1GetCommandResult>> HandleAsync(
        SubjectV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Subject>()
            .AddWhere(x => x.SubjectId == command.SubjectId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<SubjectV1GetCommandResult>.EntityNotFound(typeof(SubjectV1GetCommand));

        return new SuccessOrFailure<SubjectV1GetCommandResult>(new SubjectV1GetCommandResult
        {
            SubjectId = entity.SubjectId,
            MajorId = entity.MajorId,
            Code = entity.Code,
            Name = entity.Name,
            Ects = entity.Ects,
            DeletedAt = entity.DeletedAt,
        });
    }
}
