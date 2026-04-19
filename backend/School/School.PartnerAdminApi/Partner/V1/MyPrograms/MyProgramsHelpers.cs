using System.Security.Claims;
using System.Text;

namespace School.PartnerAdminApi.Partner.V1.MyPrograms;

internal static class MyProgramsHelpers
{
    public static async Task<(string? UserId, Guid? PartnerId)> ResolvePartnerAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return (null, null);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return (userId, user?.PartnerId);
    }

    public static string BuildPartnerPrefix(string partnerName)
    {
        if (string.IsNullOrWhiteSpace(partnerName)) return "P";
        var sb = new StringBuilder();
        foreach (var word in partnerName.Split([' ', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ch in word)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToUpperInvariant(ch));
                    break;
                }
            }
            if (sb.Length >= 6) break;
        }
        if (sb.Length == 0)
        {
            foreach (var ch in partnerName)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToUpperInvariant(ch));
                if (sb.Length >= 4) break;
            }
        }
        return sb.Length == 0 ? "P" : sb.ToString();
    }

    public static async Task<string> GenerateUniqueCodeAsync(
        OdinDbContext db, string partnerPrefix, string baseCode, CancellationToken ct)
    {
        var candidate = $"{partnerPrefix}-{baseCode}";
        var suffix = 1;
        while (await db.Programmes.AnyAsync(p => p.Code == candidate, ct))
        {
            suffix++;
            candidate = $"{partnerPrefix}-{baseCode}-{suffix}";
        }
        return candidate;
    }

    public static async Task<bool> HasEnrolmentsEverAsync(
        OdinDbContext db, Guid programmeId, CancellationToken ct) =>
        await db.StudentEnrollments
            .IgnoreQueryFilters()
            .AnyAsync(e => e.ProgrammeId == programmeId, ct);

    public static async Task<List<int>> GetCurrentPathwayIdsAsync(
        OdinDbContext db, Guid programmeId, CancellationToken ct) =>
        await db.ProgrammePathways
            .Where(pp => pp.ProgrammeId == programmeId && pp.DeletedAt == null)
            .Select(pp => pp.PathwayId)
            .ToListAsync(ct);

    public static async Task<bool> ValidatePathwayIdsAsync(
        OdinDbContext db, IReadOnlyCollection<int> pathwayIds, CancellationToken ct)
    {
        if (pathwayIds.Count == 0) return true;
        var count = await db.Pathways
            .CountAsync(p => pathwayIds.Contains(p.PathwayId) && p.DeletedAt == null, ct);
        return count == pathwayIds.Count;
    }

    public static async Task SyncProgrammePathwaysAsync(
        OdinDbContext db, Guid programmeId, IReadOnlyCollection<int> pathwayIds, CancellationToken ct)
    {
        var existing = await db.ProgrammePathways
            .Where(pp => pp.ProgrammeId == programmeId && pp.DeletedAt == null)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var row in existing.Where(r => !pathwayIds.Contains(r.PathwayId)))
        {
            row.DeletedAt = now;
        }

        var existingIds = existing.Where(r => r.DeletedAt == null).Select(r => r.PathwayId).ToHashSet();
        foreach (var pathwayId in pathwayIds.Where(id => !existingIds.Contains(id)))
        {
            db.ProgrammePathways.Add(new ProgrammePathway
            {
                ProgrammeId = programmeId,
                PathwayId = pathwayId,
            });
        }
    }
}
