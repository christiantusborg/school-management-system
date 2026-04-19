using QuVian.CaseApi.Cases.V1.Restore.Command;

namespace QuVian.CaseApi.Cases.V1.Restore.Endpoint;

[Route("/v1/cases/{caseId:guid}/restore")]
[EndpointTag("Cases")]
public sealed class CasesV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<CasesV1RestoreCommand, CasesV1RestoreEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CasesV1RestoreCommandResult, CasesV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CasesV1RestoreCommand { CaseId = caseId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
