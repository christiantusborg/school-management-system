using School.PathwayApi.Pathway.V1.Get.Command;

namespace School.PathwayApi.Pathway.V1.Get.Endpoint;

[Route("/v1/school/system-config/pathways/{id:int}")]
[EndpointTag("School.SystemConfig.Pathway")]
public sealed class PathwayV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<PathwayV1GetCommand, PathwayV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PathwayV1GetCommandResult, PathwayV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PathwayV1GetCommand { PathwayId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
