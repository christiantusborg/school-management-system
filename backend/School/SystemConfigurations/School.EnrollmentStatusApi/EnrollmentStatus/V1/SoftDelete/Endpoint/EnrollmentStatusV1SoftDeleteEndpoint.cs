using School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Command;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.SoftDelete.Endpoint;

[Route("/v1/school/system-config/enrollment-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.EnrollmentStatus")]
public sealed class EnrollmentStatusV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<EnrollmentStatusV1SoftDeleteCommand, EnrollmentStatusV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EnrollmentStatusV1SoftDeleteCommandResult, EnrollmentStatusV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EnrollmentStatusV1SoftDeleteCommand { EnrollmentStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
