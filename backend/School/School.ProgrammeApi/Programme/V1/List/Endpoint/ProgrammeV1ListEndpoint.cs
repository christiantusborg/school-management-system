using School.ProgrammeApi.Programme.V1.List.Command;

namespace School.ProgrammeApi.Programme.V1.List.Endpoint;

[Route("/v1/school/programmes")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<ProgrammeV1ListCommand, BaseGetAllResponse<ProgrammeV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<ProgrammeV1ListCommandResultItem>, BaseGetAllResponse<ProgrammeV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken,
        [FromQuery] bool deleted = false,
        [FromQuery] string? ownership = null,
        [FromQuery] string? status = null)
    {
        var command = new ProgrammeV1ListCommand { DeletedOnly = deleted, Ownership = ownership, Status = status };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
