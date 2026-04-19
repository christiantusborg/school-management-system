using School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Command;

namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Create.Endpoint;

[Route("/v1/school/system-config/final-project-statuses")]
[EndpointTag("School.SystemConfig.FinalProjectStatus")]
public sealed class FinalProjectStatusV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<FinalProjectStatusV1CreateCommand, FinalProjectStatusV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] FinalProjectStatusV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<FinalProjectStatusV1CreateEndpointRequest, FinalProjectStatusV1CreateCommand> requestMapper,
        [FromServices] IMapper<FinalProjectStatusV1CreateCommandResult, FinalProjectStatusV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
