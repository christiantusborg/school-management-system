using School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Command;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Restore.Endpoint;

[Route("/v1/school/system-config/final-project-statuses/{id:int}/restore")]
[EndpointTag("School.SystemConfig.FinalProjectStatus")]
public sealed class FinalProjectStatusV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<FinalProjectStatusV1RestoreCommand, FinalProjectStatusV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<FinalProjectStatusV1RestoreCommandResult, FinalProjectStatusV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new FinalProjectStatusV1RestoreCommand { FinalProjectStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
