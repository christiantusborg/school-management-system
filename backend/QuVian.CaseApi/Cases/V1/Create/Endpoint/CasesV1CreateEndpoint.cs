using QuVian.CaseApi.Cases.V1.Create.Command;

namespace QuVian.CaseApi.Cases.V1.Create.Endpoint;

[Route("/v1/cases")]
[EndpointTag("Cases")]
public sealed class CasesV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<CasesV1CreateCommand, CasesV1CreateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] CasesV1CreateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CasesV1CreateEndpointRequest, CasesV1CreateCommand> requestMapper,
        [FromServices] IMapper<CasesV1CreateCommandResult, CasesV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with
        {
            CreatedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
