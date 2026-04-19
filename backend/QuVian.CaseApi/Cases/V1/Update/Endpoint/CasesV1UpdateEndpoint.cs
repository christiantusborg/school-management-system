using QuVian.CaseApi.Cases.V1.Update.Command;

namespace QuVian.CaseApi.Cases.V1.Update.Endpoint;

[Route("/v1/cases/{caseId:guid}")]
[EndpointTag("Cases")]
public sealed class CasesV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<CasesV1UpdateCommand, CasesV1UpdateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        [FromBody] CasesV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CasesV1UpdateEndpointRequest, CasesV1UpdateCommand> requestMapper,
        [FromServices] IMapper<CasesV1UpdateCommandResult, CasesV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { CaseId = caseId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
