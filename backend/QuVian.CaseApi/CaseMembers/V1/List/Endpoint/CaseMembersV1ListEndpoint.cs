using QuVian.CaseApi.CaseMembers.V1.List.Command;

namespace QuVian.CaseApi.CaseMembers.V1.List.Endpoint;

[Route("/v1/cases/{caseId:guid}/members")]
[EndpointTag("CaseMembers")]
public sealed class CaseMembersV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<CaseMembersV1ListCommand, CaseMembersV1ListEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseMembersV1ListCommandResult, CaseMembersV1ListEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseMembersV1ListCommand { CaseId = caseId };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToResult(responseMapper);
    }
}
