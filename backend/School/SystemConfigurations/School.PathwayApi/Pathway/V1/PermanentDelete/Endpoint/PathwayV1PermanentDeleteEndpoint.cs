using School.PathwayApi.Pathway.V1.PermanentDelete.Command;

namespace School.PathwayApi.Pathway.V1.PermanentDelete.Endpoint;

[Route("/v1/school/system-config/pathways/{id:int}/permanent")]
[EndpointTag("School.SystemConfig.Pathway")]
public sealed class PathwayV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<PathwayV1PermanentDeleteCommand, PathwayV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PathwayV1PermanentDeleteCommandResult, PathwayV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PathwayV1PermanentDeleteCommand { PathwayId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
