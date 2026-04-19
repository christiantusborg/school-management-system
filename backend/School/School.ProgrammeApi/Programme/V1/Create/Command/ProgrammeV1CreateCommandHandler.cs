namespace School.ProgrammeApi.Programme.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1CreateCommandHandler(IProgrammeRepository repository, OdinDbContext db)
    : ICommandHandler<ProgrammeV1CreateCommand, ProgrammeV1CreateCommandResult,
        SharedLibrary.Basics.Opaque.Domains.Programme, IProgrammeRepository>
{
    public async Task<SuccessOrFailure<ProgrammeV1CreateCommandResult>> HandleAsync(
        ProgrammeV1CreateCommand command, CancellationToken cancellationToken)
    {
        var distinctPathwayIds = command.PathwayIds.Where(id => id > 0).Distinct().ToList();
        if (distinctPathwayIds.Count > 0)
        {
            var existingCount = await db.Pathways
                .CountAsync(p => distinctPathwayIds.Contains(p.PathwayId) && p.DeletedAt == null,
                    cancellationToken)
                .ConfigureAwait(false);
            if (existingCount != distinctPathwayIds.Count)
                return SuccessOrFailureHelper<ProgrammeV1CreateCommandResult>.Create("Invalid_PathwayIds");
        }

        var entity = new SharedLibrary.Basics.Opaque.Domains.Programme
        {
            Name = command.Name,
            Code = command.Code,
        };
        repository.Add(entity);

        foreach (var pathwayId in distinctPathwayIds)
        {
            db.ProgrammePathways.Add(new ProgrammePathway
            {
                ProgrammeId = entity.ProgrammeId,
                PathwayId = pathwayId,
            });
        }

        return new SuccessOrFailure<ProgrammeV1CreateCommandResult>(
            new ProgrammeV1CreateCommandResult { ProgrammeId = entity.ProgrammeId });
    }
}
