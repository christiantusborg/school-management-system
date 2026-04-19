using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Command;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Get.Endpoint;

[Route("/v1/school/system-config/tuition-fee-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.TuitionFeeStatus")]
public sealed class TuitionFeeStatusV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<TuitionFeeStatusV1GetCommand, TuitionFeeStatusV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<TuitionFeeStatusV1GetCommandResult, TuitionFeeStatusV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new TuitionFeeStatusV1GetCommand { TuitionFeeStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
