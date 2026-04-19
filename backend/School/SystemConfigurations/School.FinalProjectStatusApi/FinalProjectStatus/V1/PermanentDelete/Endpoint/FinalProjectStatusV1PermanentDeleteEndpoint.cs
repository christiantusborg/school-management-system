using School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Command;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.PermanentDelete.Endpoint;

[Route("/v1/school/system-config/final-project-statuses/{id:int}/permanent")]
[EndpointTag("School.SystemConfig.FinalProjectStatus")]
public sealed class FinalProjectStatusV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<FinalProjectStatusV1PermanentDeleteCommand, FinalProjectStatusV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<FinalProjectStatusV1PermanentDeleteCommandResult, FinalProjectStatusV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new FinalProjectStatusV1PermanentDeleteCommand { FinalProjectStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
