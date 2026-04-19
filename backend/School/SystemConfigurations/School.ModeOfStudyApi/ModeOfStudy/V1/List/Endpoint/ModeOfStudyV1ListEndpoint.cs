using School.ModeOfStudyApi.ModeOfStudy.V1.List.Command;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.List.Endpoint;

[Route("/v1/school/system-config/modes-of-study")]
[EndpointTag("School.SystemConfig.ModeOfStudy")]
public sealed class ModeOfStudyV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<ModeOfStudyV1ListCommand, BaseGetAllResponse<ModeOfStudyV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<ModeOfStudyV1ListCommandResultItem>, BaseGetAllResponse<ModeOfStudyV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ModeOfStudyV1ListCommand();
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
