using School.SubjectApi.Subject.V1.List.Command;

namespace School.SubjectApi.Subject.V1.List.Endpoint;

[Route("/v1/school/subjects")]
[EndpointTag("School.Subject")]
public sealed class SubjectV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<SubjectV1ListCommand, BaseGetAllResponse<SubjectV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<SubjectV1ListCommandResultItem>, BaseGetAllResponse<SubjectV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken,
        [FromQuery] bool deleted = false)
    {
        var command = new SubjectV1ListCommand { DeletedOnly = deleted };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
