using System.Text;
using System.Text.RegularExpressions;

namespace School.PartnerAdminApi.Partner.V1;

internal static class PartnerSlugHelpers
{
    private static readonly Regex SlugPattern = new(
        "^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$",
        RegexOptions.Compiled);

    public static bool IsValid(string? slug) =>
        !string.IsNullOrEmpty(slug) && slug.Length is >= 2 and <= 40 && SlugPattern.IsMatch(slug);

    public static string FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "p";
        var sb = new StringBuilder();
        foreach (var ch in name.Normalize().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (sb.Length > 0 && sb[^1] != '-') sb.Append('-');
        }
        var slug = sb.ToString().Trim('-');
        if (slug.Length < 2) slug = $"{slug}-p";
        return slug.Length > 40 ? slug[..40].TrimEnd('-') : slug;
    }

    public static async Task<string> EnsureUniqueAsync(
        OdinDbContext db, string baseSlug, Guid? selfPartnerId, CancellationToken ct)
    {
        var candidate = baseSlug;
        var suffix = 1;
        while (await db.Partners.AnyAsync(p =>
            p.Slug == candidate
            && p.DeletedAt == null
            && (selfPartnerId == null || p.PartnerId != selfPartnerId), ct))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";
            if (candidate.Length > 40) candidate = $"{baseSlug[..(40 - 1 - suffix.ToString().Length)]}-{suffix}";
        }
        return candidate;
    }
}
