using QuVian.CaseApi.Cases.V1.List.Command;

namespace QuVian.CaseApi.Cases.V1.List.Endpoint;

[Route("/v1/cases")]
[EndpointTag("Cases")]
public sealed class CasesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<CasesV1ListCommand, BaseGetAllResponse<CasesV1ListEndpointResponseItem>>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        CaseStatus? status,
        CasePriority? priority,
        int page,
        int pageSize,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<CasesV1ListCommandResultItem>, BaseGetAllResponse<CasesV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CasesV1ListCommand
        {
            Status = status,
            Priority = priority,
            Page = page == 0 ? 1 : page,
            PageSize = pageSize == 0 ? 20 : pageSize
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
