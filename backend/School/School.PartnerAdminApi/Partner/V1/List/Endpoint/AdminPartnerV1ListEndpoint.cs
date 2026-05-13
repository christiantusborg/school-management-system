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
        CancellationToken ct)
    {
        // Read the query param directly off the request so the binder doesn't
        // reject the call when it's omitted entirely (the default state).
        var includeDeleted = httpContext.Request.Query.TryGetValue("includeDeleted", out var v)
            && bool.TryParse(v.ToString(), out var parsed) && parsed;
        var result = await sender.SendAsync(new AdminPartnerV1ListCommand(includeDeleted), ct);
        return result.ToResult(mapper);
    }
}
