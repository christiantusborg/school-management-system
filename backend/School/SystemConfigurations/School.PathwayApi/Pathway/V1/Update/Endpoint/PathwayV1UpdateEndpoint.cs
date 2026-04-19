using School.PathwayApi.Pathway.V1.Update.Command;

namespace School.PathwayApi.Pathway.V1.Update.Endpoint;

[Route("/v1/school/system-config/pathways/{id:int}")]
[EndpointTag("School.SystemConfig.Pathway")]
public sealed class PathwayV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<PathwayV1UpdateCommand, PathwayV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromBody] PathwayV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PathwayV1UpdateEndpointRequest, PathwayV1UpdateCommand> requestMapper,
        [FromServices] IMapper<PathwayV1UpdateCommandResult, PathwayV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { PathwayId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
