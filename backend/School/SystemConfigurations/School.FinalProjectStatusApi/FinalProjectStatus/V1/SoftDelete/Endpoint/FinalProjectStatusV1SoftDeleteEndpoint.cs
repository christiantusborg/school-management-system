using School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Command;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.SoftDelete.Endpoint;

[Route("/v1/school/system-config/final-project-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.FinalProjectStatus")]
public sealed class FinalProjectStatusV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<FinalProjectStatusV1SoftDeleteCommand, FinalProjectStatusV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<FinalProjectStatusV1SoftDeleteCommandResult, FinalProjectStatusV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new FinalProjectStatusV1SoftDeleteCommand { FinalProjectStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
