using School.PathwayApi.Pathway.V1.Create.Command;

namespace School.PathwayApi.Pathway.V1.Create.Endpoint;

[Route("/v1/school/system-config/pathways")]
[EndpointTag("School.SystemConfig.Pathway")]
public sealed class PathwayV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<PathwayV1CreateCommand, PathwayV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] PathwayV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PathwayV1CreateEndpointRequest, PathwayV1CreateCommand> requestMapper,
        [FromServices] IMapper<PathwayV1CreateCommandResult, PathwayV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
