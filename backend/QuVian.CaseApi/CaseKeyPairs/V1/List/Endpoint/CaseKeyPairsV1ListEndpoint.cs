using QuVian.CaseApi.CaseKeyPairs.V1.List.Command;

namespace QuVian.CaseApi.CaseKeyPairs.V1.List.Endpoint;

[Route("/v1/cases/{caseId:guid}/key-pairs")]
[EndpointTag("CaseKeyPairs")]
public sealed class CaseKeyPairsV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<CaseKeyPairsV1ListCommand, CaseKeyPairsV1ListEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseKeyPairsV1ListCommandResult, CaseKeyPairsV1ListEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseKeyPairsV1ListCommand { CaseId = caseId };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToResult(responseMapper);
    }
}
