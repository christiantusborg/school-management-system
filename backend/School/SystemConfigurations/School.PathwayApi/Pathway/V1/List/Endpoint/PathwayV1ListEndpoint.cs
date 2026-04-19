using School.PathwayApi.Pathway.V1.List.Command;

namespace School.PathwayApi.Pathway.V1.List.Endpoint;

[Route("/v1/school/system-config/pathways")]
[EndpointTag("School.SystemConfig.Pathway")]
public sealed class PathwayV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<PathwayV1ListCommand, BaseGetAllResponse<PathwayV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<PathwayV1ListCommandResultItem>, BaseGetAllResponse<PathwayV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PathwayV1ListCommand();
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
