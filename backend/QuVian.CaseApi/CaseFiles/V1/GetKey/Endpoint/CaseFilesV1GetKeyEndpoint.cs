using QuVian.CaseApi.CaseFiles.V1.GetKey.Command;

namespace QuVian.CaseApi.CaseFiles.V1.GetKey.Endpoint;

[Route("/v1/cases/{caseId:guid}/files/{caseFileId:guid}/key")]
[EndpointTag("CaseFiles")]
public sealed class CaseFilesV1GetKeyEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<CaseFilesV1GetKeyCommand, CaseFilesV1GetKeyEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId, Guid caseFileId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseFilesV1GetKeyCommandResult, CaseFilesV1GetKeyEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseFilesV1GetKeyCommand
        {
            CaseId     = caseId,
            CaseFileId = caseFileId,
            UserId     = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToResult(responseMapper);
    }
}
