using School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Command;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.PermanentDelete.Endpoint;

[Route("/v1/school/system-config/enrollment-statuses/{id:int}/permanent")]
[EndpointTag("School.SystemConfig.EnrollmentStatus")]
public sealed class EnrollmentStatusV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<EnrollmentStatusV1PermanentDeleteCommand, EnrollmentStatusV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EnrollmentStatusV1PermanentDeleteCommandResult, EnrollmentStatusV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EnrollmentStatusV1PermanentDeleteCommand { EnrollmentStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
