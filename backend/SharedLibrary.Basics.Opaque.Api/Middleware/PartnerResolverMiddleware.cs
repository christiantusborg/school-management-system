using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Odin.Api.Base.Data;
using System.Text.RegularExpressions;

namespace SharedLibrary.Basics.Opaque.Api.Middleware;

public sealed class PartnerResolverMiddleware
{
    public const string PartnerIdItemKey = "PartnerId";
    public const string PartnerSlugItemKey = "PartnerSlug";
    private static readonly Regex SubdomainRegex = new("^([a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\\.ibss\\.com$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SlugRegex = new("^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$",
        RegexOptions.Compiled);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly RequestDelegate _next;

    public PartnerResolverMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, OdinDbContext db, IMemoryCache cache)
    {
        var slug = ExtractSlug(context);
        if (!string.IsNullOrEmpty(slug))
        {
            var key = $"partner-slug:{slug}";
            var partnerId = await cache.GetOrCreateAsync(key, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                return await db.Partners
                    .Where(p => p.Slug == slug && p.DeletedAt == null)
                    .Select(p => (Guid?)p.PartnerId)
                    .FirstOrDefaultAsync(context.RequestAborted)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);

            if (partnerId is not null)
            {
                context.Items[PartnerIdItemKey] = partnerId.Value;
                context.Items[PartnerSlugItemKey] = slug;
            }
        }

        await _next(context).ConfigureAwait(false);
    }

    private static string? ExtractSlug(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            var match = SubdomainRegex.Match(host);
            if (match.Success) return match.Groups[1].Value.ToLowerInvariant();
        }

        if (context.Request.Query.TryGetValue("partner", out var qs))
        {
            var candidate = qs.ToString().Trim().ToLowerInvariant();
            if (SlugRegex.IsMatch(candidate)) return candidate;
        }

        return null;
    }
}
