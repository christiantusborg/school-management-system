using School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Command;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Endpoint;

[Route("/v1/school/system-config/final-project-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.FinalProjectStatus")]
public sealed class FinalProjectStatusV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<FinalProjectStatusV1GetCommand, FinalProjectStatusV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<FinalProjectStatusV1GetCommandResult, FinalProjectStatusV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new FinalProjectStatusV1GetCommand { FinalProjectStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
