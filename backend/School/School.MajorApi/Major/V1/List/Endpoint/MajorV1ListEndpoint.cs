using School.MajorApi.Major.V1.List.Command;

namespace School.MajorApi.Major.V1.List.Endpoint;

[Route("/v1/school/majors")]
[EndpointTag("School.Major")]
public sealed class MajorV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<MajorV1ListCommand, BaseGetAllResponse<MajorV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<MajorV1ListCommandResultItem>, BaseGetAllResponse<MajorV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken,
        [FromQuery] bool deleted = false)
    {
        var command = new MajorV1ListCommand { DeletedOnly = deleted };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
