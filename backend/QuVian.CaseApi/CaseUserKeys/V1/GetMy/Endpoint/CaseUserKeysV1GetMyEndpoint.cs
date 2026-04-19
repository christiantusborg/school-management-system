using QuVian.CaseApi.CaseUserKeys.V1.GetMy.Command;

namespace QuVian.CaseApi.CaseUserKeys.V1.GetMy.Endpoint;

[Route("/v1/cases/{caseId:guid}/my-keys")]
[EndpointTag("CaseUserKeys")]
public sealed class CaseUserKeysV1GetMyEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<CaseUserKeysV1GetMyCommand, CaseUserKeysV1GetMyEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseUserKeysV1GetMyCommandResult, CaseUserKeysV1GetMyEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseUserKeysV1GetMyCommand
        {
            CaseId = caseId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToResult(responseMapper);
    }
}
