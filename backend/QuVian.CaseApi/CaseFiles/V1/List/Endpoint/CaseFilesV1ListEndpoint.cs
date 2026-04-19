using QuVian.CaseApi.CaseFiles.V1.List.Command;

namespace QuVian.CaseApi.CaseFiles.V1.List.Endpoint;

[Route("/v1/cases/{caseId:guid}/files")]
[EndpointTag("CaseFiles")]
public sealed class CaseFilesV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<CaseFilesV1ListCommand, CaseFilesV1ListEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId, ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseFilesV1ListCommandResult, CaseFilesV1ListEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseFilesV1ListCommand
        {
            CaseId = caseId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToResult(responseMapper);
    }
}
