using School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Command;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Restore.Endpoint;

[Route("/v1/school/system-config/enrollment-statuses/{id:int}/restore")]
[EndpointTag("School.SystemConfig.EnrollmentStatus")]
public sealed class EnrollmentStatusV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<EnrollmentStatusV1RestoreCommand, EnrollmentStatusV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EnrollmentStatusV1RestoreCommandResult, EnrollmentStatusV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EnrollmentStatusV1RestoreCommand { EnrollmentStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
