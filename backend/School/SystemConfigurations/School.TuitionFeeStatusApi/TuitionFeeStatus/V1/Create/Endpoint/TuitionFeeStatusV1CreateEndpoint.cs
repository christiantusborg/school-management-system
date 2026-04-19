using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Command;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Create.Endpoint;

[Route("/v1/school/system-config/tuition-fee-statuses")]
[EndpointTag("School.SystemConfig.TuitionFeeStatus")]
public sealed class TuitionFeeStatusV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<TuitionFeeStatusV1CreateCommand, TuitionFeeStatusV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] TuitionFeeStatusV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<TuitionFeeStatusV1CreateEndpointRequest, TuitionFeeStatusV1CreateCommand> requestMapper,
        [FromServices] IMapper<TuitionFeeStatusV1CreateCommandResult, TuitionFeeStatusV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
