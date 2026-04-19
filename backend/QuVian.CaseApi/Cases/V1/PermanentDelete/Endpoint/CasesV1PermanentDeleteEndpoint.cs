using QuVian.CaseApi.Cases.V1.PermanentDelete.Command;

namespace QuVian.CaseApi.Cases.V1.PermanentDelete.Endpoint;

[Route("/v1/cases/{caseId:guid}/permanent")]
[EndpointTag("Cases")]
public sealed class CasesV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<CasesV1PermanentDeleteCommand, CasesV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CasesV1PermanentDeleteCommandResult, CasesV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CasesV1PermanentDeleteCommand { CaseId = caseId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
