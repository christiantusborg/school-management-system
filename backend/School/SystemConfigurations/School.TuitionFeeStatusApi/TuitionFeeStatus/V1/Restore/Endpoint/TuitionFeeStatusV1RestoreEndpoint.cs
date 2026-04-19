using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Command;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.Restore.Endpoint;

[Route("/v1/school/system-config/tuition-fee-statuses/{id:int}/restore")]
[EndpointTag("School.SystemConfig.TuitionFeeStatus")]
public sealed class TuitionFeeStatusV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<TuitionFeeStatusV1RestoreCommand, TuitionFeeStatusV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<TuitionFeeStatusV1RestoreCommandResult, TuitionFeeStatusV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new TuitionFeeStatusV1RestoreCommand { TuitionFeeStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
