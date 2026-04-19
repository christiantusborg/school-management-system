using School.PathwayApi.Pathway.V1.Restore.Command;

namespace School.PathwayApi.Pathway.V1.Restore.Endpoint;

[Route("/v1/school/system-config/pathways/{id:int}/restore")]
[EndpointTag("School.SystemConfig.Pathway")]
public sealed class PathwayV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<PathwayV1RestoreCommand, PathwayV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PathwayV1RestoreCommandResult, PathwayV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PathwayV1RestoreCommand { PathwayId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
