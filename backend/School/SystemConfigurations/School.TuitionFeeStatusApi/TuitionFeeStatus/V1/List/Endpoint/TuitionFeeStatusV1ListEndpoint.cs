using School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Command;

namespace School.TuitionFeeStatusApi.TuitionFeeStatus.V1.List.Endpoint;

[Route("/v1/school/system-config/tuition-fee-statuses")]
[EndpointTag("School.SystemConfig.TuitionFeeStatus")]
public sealed class TuitionFeeStatusV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<TuitionFeeStatusV1ListCommand, BaseGetAllResponse<TuitionFeeStatusV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<TuitionFeeStatusV1ListCommandResultItem>, BaseGetAllResponse<TuitionFeeStatusV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new TuitionFeeStatusV1ListCommand();
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
