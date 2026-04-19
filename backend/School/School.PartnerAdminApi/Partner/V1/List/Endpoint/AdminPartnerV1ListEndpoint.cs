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
        CancellationToken ct)
    {
        var result = await sender.SendAsync(new AdminPartnerV1ListCommand(), ct);
        return result.ToResult(mapper);
    }
}
