using Microsoft.EntityFrameworkCore;
using Odin.Api.Base.Data;

namespace School.ProgrammeApi.Programme.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProgrammeV1ListCommandHandler(OdinDbContext db)
    : ICommandHandler<ProgrammeV1ListCommand, CommandSearchResult<ProgrammeV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.Programme, IProgrammeRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<ProgrammeV1ListCommandResultItem>>> HandleAsync(
        ProgrammeV1ListCommand command, CancellationToken cancellationToken)
    {
        var query = db.Programmes.AsQueryable();
        query = command.DeletedOnly
            ? query.Where(p => p.DeletedAt != null)
            : query.Where(p => p.DeletedAt == null);

        if (string.Equals(command.Ownership, "core", StringComparison.OrdinalIgnoreCase))
            query = query.Where(p => p.PartnerId == null);
        else if (string.Equals(command.Ownership, "partner", StringComparison.OrdinalIgnoreCase))
            query = query.Where(p => p.PartnerId != null);

        if (!string.IsNullOrWhiteSpace(command.Status)
            && Enum.TryParse<ProgrammeStatus>(command.Status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        var rows = await query
            .Select(p => new
            {
                p.ProgrammeId,
                p.Name,
                p.Code,
                p.DeletedAt,
                p.PartnerId,
                PartnerName = p.Partner != null ? p.Partner.Name : null,
                p.Status,
                p.IsActive,
                p.IsDisabledByAdmin,
                p.RejectionReason,
                p.SubmittedAt,
                p.ApprovedAt,
            })
            .ToListAsync(cancellationToken);

        var programmeIds = rows.Select(r => r.ProgrammeId).ToList();
        var enrolledIds = await db.StudentEnrollments
            .IgnoreQueryFilters()
            .Where(e => programmeIds.Contains(e.ProgrammeId))
            .Select(e => e.ProgrammeId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var enrolledSet = enrolledIds.ToHashSet();

        var pathwayLinks = await db.ProgrammePathways
            .Where(pp => programmeIds.Contains(pp.ProgrammeId) && pp.DeletedAt == null)
            .Select(pp => new { pp.ProgrammeId, pp.PathwayId })
            .ToListAsync(cancellationToken);
        var pathwaysByProgramme = pathwayLinks
            .GroupBy(pp => pp.ProgrammeId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<int>)g.Select(x => x.PathwayId).ToList());

        var items = rows
            .OrderBy(r => r.Name)
            .Select(r => new ProgrammeV1ListCommandResultItem
            {
                ProgrammeId = r.ProgrammeId,
                Name = r.Name,
                Code = r.Code,
                DeletedAt = r.DeletedAt,
                PartnerId = r.PartnerId,
                PartnerName = r.PartnerName,
                Status = r.Status.ToString(),
                IsActive = r.IsActive,
                IsDisabledByAdmin = r.IsDisabledByAdmin,
                RejectionReason = r.RejectionReason,
                SubmittedAt = r.SubmittedAt,
                ApprovedAt = r.ApprovedAt,
                HasEnrolments = enrolledSet.Contains(r.ProgrammeId),
                PathwayIds = pathwaysByProgramme.TryGetValue(r.ProgrammeId, out var pids) ? pids : [],
            })
            .ToList();

        return new SuccessOrFailure<CommandSearchResult<ProgrammeV1ListCommandResultItem>>(
            new CommandSearchResult<ProgrammeV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}
