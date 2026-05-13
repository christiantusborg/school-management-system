using System.Security.Claims;

namespace School.PartnerAdminApi.Partner.V1.MyUsers;

internal static class MyUsersHelpers
{
    public static async Task<(string? UserId, Guid? PartnerId, IResult? Failure)> ResolveAsync(
        HttpContext httpContext, OdinDbContext db, CancellationToken ct)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return (null, null, Results.Unauthorized());
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user?.PartnerId is null) return (null, null, Results.Forbid());
        return (userId, user.PartnerId, null);
    }
}
