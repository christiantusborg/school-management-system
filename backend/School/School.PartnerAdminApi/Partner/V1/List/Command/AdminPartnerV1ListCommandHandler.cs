namespace School.PartnerAdminApi.Partner.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminPartnerV1ListCommandHandler(OdinDbContext db)
    : ICommandHandler<AdminPartnerV1ListCommand, CommandSearchResult<AdminPartnerV1ListCommandResultItem>,
        SharedLibrary.Basics.Opaque.Domains.Partners.Partner, IPartnerRepository>
{
    public async Task<SuccessOrFailure<CommandSearchResult<AdminPartnerV1ListCommandResultItem>>> HandleAsync(
        AdminPartnerV1ListCommand command, CancellationToken cancellationToken)
    {
        // Hide soft-deleted partners by default. Admin can pass
        // ?includeDeleted=true to see them (drives the "Show deleted" toggle).
        var query = db.Partners.AsQueryable();
        if (!command.IncludeDeleted) query = query.Where(p => p.DeletedAt == null);
        var partners = await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var partnerIds = partners.Select(p => p.PartnerId).ToList();

        // Exclude users in the Student role so the partner-org user count matches what
        // the Users tab shows (partner staff only — students live under the Students tab).
        var studentRoleId = await db.Roles
            .Where(r => r.Name == "Student")
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var userCounts = await db.Users
            .Where(u => u.PartnerId != null && partnerIds.Contains(u.PartnerId!.Value) && u.DeletedAt == null)
            .Where(u => studentRoleId == null
                || !db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == studentRoleId))
            .GroupBy(u => u.PartnerId!.Value)
            .Select(g => new { PartnerId = g.Key, Count = g.Count(), AnyEnabled = g.Any(u => u.IsEnabled) })
            .ToListAsync(cancellationToken);

        var countMap = userCounts.ToDictionary(x => x.PartnerId);

        var items = partners.Select(p => new AdminPartnerV1ListCommandResultItem
        {
            PartnerId  = p.PartnerId,
            Name       = p.Name,
            Slug       = p.Slug,
            UserCount  = countMap.TryGetValue(p.PartnerId, out var c) ? c.Count : 0,
            // Enabled = not disabled AND not deleted.
            IsEnabled  = p.DisabledAt == null && p.DeletedAt == null,
            DisabledAt = p.DisabledAt,
            DeletedAt  = p.DeletedAt,
        }).ToList();

        return new SuccessOrFailure<CommandSearchResult<AdminPartnerV1ListCommandResultItem>>(
            new CommandSearchResult<AdminPartnerV1ListCommandResultItem> { Items = items, Total = items.Count });
    }
}
