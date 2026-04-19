namespace School.ProgrammeApi.Programme.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1GetCommandHandler(IProgrammeRepository repository, OdinDbContext db)
    : ICommandHandler<ProgrammeV1GetCommand, ProgrammeV1GetCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Programme, IProgrammeRepository>
{
    public async Task<SuccessOrFailure<ProgrammeV1GetCommandResult>> HandleAsync(
        ProgrammeV1GetCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Programme>()
            .AddWhere(x => x.ProgrammeId == command.ProgrammeId);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ProgrammeV1GetCommandResult>.EntityNotFound(typeof(ProgrammeV1GetCommand));

        var pathwayIds = await db.ProgrammePathways
            .Where(r => r.ProgrammeId == command.ProgrammeId && r.DeletedAt == null)
            .Select(r => r.PathwayId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new SuccessOrFailure<ProgrammeV1GetCommandResult>(new ProgrammeV1GetCommandResult
        {
            ProgrammeId = entity.ProgrammeId,
            Name = entity.Name,
            Code = entity.Code,
            PathwayIds = pathwayIds,
            DeletedAt = entity.DeletedAt,
        });
    }
}
