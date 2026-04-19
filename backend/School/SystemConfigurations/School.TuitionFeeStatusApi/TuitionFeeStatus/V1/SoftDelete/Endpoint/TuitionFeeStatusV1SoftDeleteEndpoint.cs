using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Command;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.SoftDelete.Endpoint;

[Route("/v1/school/system-config/tuition-fee-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.TuitionFeeStatus")]
public sealed class TuitionFeeStatusV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<TuitionFeeStatusV1SoftDeleteCommand, TuitionFeeStatusV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<TuitionFeeStatusV1SoftDeleteCommandResult, TuitionFeeStatusV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new TuitionFeeStatusV1SoftDeleteCommand { TuitionFeeStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
