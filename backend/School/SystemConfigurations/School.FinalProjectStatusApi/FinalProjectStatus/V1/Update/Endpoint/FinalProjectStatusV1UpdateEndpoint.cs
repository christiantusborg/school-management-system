using School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Command;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Update.Endpoint;

[Route("/v1/school/system-config/final-project-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.FinalProjectStatus")]
public sealed class FinalProjectStatusV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<FinalProjectStatusV1UpdateCommand, FinalProjectStatusV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromBody] FinalProjectStatusV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<FinalProjectStatusV1UpdateEndpointRequest, FinalProjectStatusV1UpdateCommand> requestMapper,
        [FromServices] IMapper<FinalProjectStatusV1UpdateCommandResult, FinalProjectStatusV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { FinalProjectStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
