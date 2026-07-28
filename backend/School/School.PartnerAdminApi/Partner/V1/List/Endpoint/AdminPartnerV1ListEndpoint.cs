using School.PartnerAdminApi.Partner.V1.List.Command;

namespace School.PartnerAdminApi.Partner.V1.List.Endpoint;

[Route("/v1/admin/school/partners")]
[EndpointTag("Admin.School.Partner")]
public sealed class AdminPartnerV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<AdminPartnerV1ListCommand, AdminPartnerV1ListEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<AdminPartnerV1ListCommandResultItem>, AdminPartnerV1ListEndpointResponse> mapper,
        HttpContext httpContext,
        [FromServices] OdinDbContext db,
        CancellationToken ct)
    {
        // Read the query param directly off the request so the binder doesn't
        // reject the call when it's omitted entirely (the default state).
        var includeDeleted = httpContext.Request.Query.TryGetValue("includeDeleted", out var v)
            && bool.TryParse(v.ToString(), out var parsed) && parsed;
        var result = await sender.SendAsync(new AdminPartnerV1ListCommand(includeDeleted), ct);

        // Sales logins only see their assigned partners.
        if (httpContext.User.IsInRole("Sales"))
        {
            var salesUserId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var assigned = (await db.SalesPartnerAssignments
                .Where(a => a.UserId == salesUserId)
                .Select(a => a.PartnerId)
                .ToListAsync(ct)).ToHashSet();
            if (!result.TryGetResponseRaw(out var data, out var failure)) return failure!;
            var resp = mapper.MapFrom(data!);
            var items = resp.Items.Where(i => assigned.Contains(i.PartnerId)).ToList();
            return Results.Ok(new AdminPartnerV1ListEndpointResponse
            {
                Items = items,
                Total = items.Count,
                Links = [],
            });
        }
        return result.ToResult(mapper);
    }
}
