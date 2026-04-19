using School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Command;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Get.Endpoint;

[Route("/v1/school/system-config/enrollment-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.EnrollmentStatus")]
public sealed class EnrollmentStatusV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<EnrollmentStatusV1GetCommand, EnrollmentStatusV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EnrollmentStatusV1GetCommandResult, EnrollmentStatusV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EnrollmentStatusV1GetCommand { EnrollmentStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
