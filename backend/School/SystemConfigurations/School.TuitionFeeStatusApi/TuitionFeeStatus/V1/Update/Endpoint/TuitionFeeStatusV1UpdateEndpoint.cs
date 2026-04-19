using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Command;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Update.Endpoint;

[Route("/v1/school/system-config/tuition-fee-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.TuitionFeeStatus")]
public sealed class TuitionFeeStatusV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<TuitionFeeStatusV1UpdateCommand, TuitionFeeStatusV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromBody] TuitionFeeStatusV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<TuitionFeeStatusV1UpdateEndpointRequest, TuitionFeeStatusV1UpdateCommand> requestMapper,
        [FromServices] IMapper<TuitionFeeStatusV1UpdateCommandResult, TuitionFeeStatusV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { TuitionFeeStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
