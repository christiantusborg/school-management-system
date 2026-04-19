using QuVian.CaseApi.Cases.V1.Get.Command;

namespace QuVian.CaseApi.Cases.V1.Get.Endpoint;

[Route("/v1/cases/{caseId:guid}")]
[EndpointTag("Cases")]
public sealed class CasesV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<CasesV1GetCommand, CasesV1GetEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CasesV1GetCommandResult, CasesV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CasesV1GetCommand { CaseId = caseId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
