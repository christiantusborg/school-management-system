namespace School.PartnerAdminApi.Partner.V1.UpdateUser.Endpoint;

[Route("/v1/admin/school/partners/{pid:guid}/users/{uid}")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1UpdateUserEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(Route, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private static async Task<IResult> EndpointHandlerAsync(
        Guid pid, string uid,
        [FromBody] AdminPartnerV1UpdateUserEndpointRequest request,
        [FromServices] OdinDbContext db,
        [FromServices] UserManager<ApplicationUser> userManager,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == uid && u.PartnerId == pid, ct);
        if (user is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var newName = request.Username.Trim();
            if (!string.Equals(newName, user.UserName, StringComparison.Ordinal))
            {
                var existing = await userManager.FindByNameAsync(newName);
                if (existing is not null && existing.Id != user.Id)
                    return Results.BadRequest($"Username '{newName}' is already taken.");

                user.UserName = newName;
                var setResult = await userManager.UpdateAsync(user);
                if (!setResult.Succeeded)
                    return Results.BadRequest(string.Join(", ", setResult.Errors.Select(e => e.Description)));
            }
        }

        if (request.FirstName is not null || request.LastName is not null)
        {
            var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == uid, ct);
            if (profile is null)
            {
                profile = new UserProfile { UserId = uid };
                db.UserProfiles.Add(profile);
            }

            if (request.FirstName is not null)
                profile.FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim();
            if (request.LastName is not null)
                profile.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();
        }

        await db.SaveChangesAsync(ct);

        var profileOut = await db.UserProfiles
            .Where(p => p.UserId == uid)
            .Select(p => new { p.FirstName, p.LastName })
            .FirstOrDefaultAsync(ct);

        return Results.Ok(new
        {
            userId    = user.Id,
            username  = user.UserName,
            firstName = profileOut?.FirstName,
            lastName  = profileOut?.LastName,
        });
    }

    private const string Route = "/v1/admin/school/partners/{pid:guid}/users/{uid}";
}
