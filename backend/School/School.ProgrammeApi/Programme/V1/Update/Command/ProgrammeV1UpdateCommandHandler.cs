namespace School.ProgrammeApi.Programme.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1UpdateCommandHandler(IProgrammeRepository repository, OdinDbContext db)
    : ICommandHandler<ProgrammeV1UpdateCommand, ProgrammeV1UpdateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Programme, IProgrammeRepository>
{
    public async Task<SuccessOrFailure<ProgrammeV1UpdateCommandResult>> HandleAsync(
        ProgrammeV1UpdateCommand command, CancellationToken cancellationToken)
    {
        var spec = new Specification<SharedLibrary.Basics.Opaque.Domains.Programme>()
            .AddWhere(x => x.ProgrammeId == command.ProgrammeId)
            .AddWhere(x => x.DeletedAt == null);

        var entity = await repository.GetAsync(spec, cancellationToken).ConfigureAwait(false);

        if (entity is null)
            return SuccessOrFailureHelper<ProgrammeV1UpdateCommandResult>.EntityNotFound(typeof(ProgrammeV1UpdateCommand));

        var distinctPathwayIds = command.PathwayIds.Where(id => id > 0).Distinct().ToList();
        if (distinctPathwayIds.Count > 0)
        {
            var existingCount = await db.Pathways
                .CountAsync(p => distinctPathwayIds.Contains(p.PathwayId) && p.DeletedAt == null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existingCount != distinctPathwayIds.Count)
                return SuccessOrFailureHelper<ProgrammeV1UpdateCommandResult>.Create("Invalid_PathwayIds");
        }

        entity.Name = command.Name;
        entity.Code = command.Code;

        var existing = await db.ProgrammePathways
            .Where(r => r.ProgrammeId == command.ProgrammeId && r.DeletedAt == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        foreach (var row in existing.Where(r => !distinctPathwayIds.Contains(r.PathwayId)))
        {
            row.DeletedAt = now;
        }

        var existingIds = existing.Where(r => r.DeletedAt == null).Select(r => r.PathwayId).ToHashSet();
        foreach (var pathwayId in distinctPathwayIds.Where(id => !existingIds.Contains(id)))
        {
            db.ProgrammePathways.Add(new ProgrammePathway
            {
                ProgrammeId = command.ProgrammeId,
                PathwayId = pathwayId,
            });
        }

        return new SuccessOrFailure<ProgrammeV1UpdateCommandResult>(
            new ProgrammeV1UpdateCommandResult { ProgrammeId = entity.ProgrammeId });
    }
}
