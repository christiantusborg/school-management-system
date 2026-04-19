using School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Command;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.List.Endpoint;

[Route("/v1/school/system-config/final-project-statuses")]
[EndpointTag("School.SystemConfig.FinalProjectStatus")]
public sealed class FinalProjectStatusV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<FinalProjectStatusV1ListCommand, BaseGetAllResponse<FinalProjectStatusV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<FinalProjectStatusV1ListCommandResultItem>, BaseGetAllResponse<FinalProjectStatusV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new FinalProjectStatusV1ListCommand();
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
