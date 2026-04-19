using School.PathwayApi.Pathway.V1.SoftDelete.Command;

namespace School.PathwayApi.Pathway.V1.SoftDelete.Endpoint;

[Route("/v1/school/system-config/pathways/{id:int}")]
[EndpointTag("School.SystemConfig.Pathway")]
public sealed class PathwayV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<PathwayV1SoftDeleteCommand, PathwayV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PathwayV1SoftDeleteCommandResult, PathwayV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PathwayV1SoftDeleteCommand { PathwayId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
