namespace School.PartnerAdminApi.Partner.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminPartnerV1ListCommandHandler(OdinDbContext db)
    : ICommandHandler<AdminPartnerV1ListCommand, CommandSearchResult<AdminPartnerV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.Partner, IPartnerRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<AdminPartnerV1ListCommandResultItem>>> HandleAsync(
        AdminPartnerV1ListCommand command, CancellationToken cancellationToken)
    {
        var partners = await db.Partners
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var partnerIds = partners.Select(p => p.PartnerId).ToList();

        var userCounts = await db.Users
            .Where(u => u.PartnerId != null && partnerIds.Contains(u.PartnerId!.Value))
            .GroupBy(u => u.PartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Count = g.Count(), AnyEnabled = g.Any(u => u.IsEnabled) })
            .ToListAsync(cancellationToken);

        var countMap = userCounts.ToDictionary(x => x.PartnerId);

        var items = partners.Select(p => new AdminPartnerV1ListCommandResultItem
        {
            PartnerId  = p.PartnerId,
            Name       = p.Name,
            UserCount  = countMap.TryGetValue(p.PartnerId, out var c) ? c.Count : 0,
            IsEnabled  = p.DeletedAt == null,
            DeletedAt  = p.DeletedAt,
        }).ToList();

        return new SuccessOrFailure<CommandSearchResult<AdminPartnerV1ListCommandResultItem>>(
            new CommandSearchResult<AdminPartnerV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}
