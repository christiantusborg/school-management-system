using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Command;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.PermanentDelete.Endpoint;

[Route("/v1/school/system-config/tuition-fee-statuses/{id:int}/permanent")]
[EndpointTag("School.SystemConfig.TuitionFeeStatus")]
public sealed class TuitionFeeStatusV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<TuitionFeeStatusV1PermanentDeleteCommand, TuitionFeeStatusV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<TuitionFeeStatusV1PermanentDeleteCommandResult, TuitionFeeStatusV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new TuitionFeeStatusV1PermanentDeleteCommand { TuitionFeeStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
