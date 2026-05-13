using School.PathwayApi.EducationLevel.V1.List.Command;

namespace School.PathwayApi.EducationLevel.V1.List.Endpoint;

[Route("/v1/school/system-config/education-levels")]
[EndpointTag("School.SystemConfig.EducationLevel")]
public sealed class EducationLevelV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<EducationLevelV1ListCommand, BaseGetAllResponse<EducationLevelV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<EducationLevelV1ListCommandResultItem>, BaseGetAllResponse<EducationLevelV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EducationLevelV1ListCommand();
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
