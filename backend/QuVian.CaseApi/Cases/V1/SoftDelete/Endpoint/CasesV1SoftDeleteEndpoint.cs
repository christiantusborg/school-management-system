using QuVian.CaseApi.Cases.V1.SoftDelete.Command;

namespace QuVian.CaseApi.Cases.V1.SoftDelete.Endpoint;

[Route("/v1/cases/{caseId:guid}")]
[EndpointTag("Cases")]
public sealed class CasesV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<CasesV1SoftDeleteCommand, CasesV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CasesV1SoftDeleteCommandResult, CasesV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CasesV1SoftDeleteCommand { CaseId = caseId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
