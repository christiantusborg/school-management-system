using School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Command;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Update.Endpoint;

[Route("/v1/school/system-config/enrollment-statuses/{id:int}")]
[EndpointTag("School.SystemConfig.EnrollmentStatus")]
public sealed class EnrollmentStatusV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<EnrollmentStatusV1UpdateCommand, EnrollmentStatusV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromBody] EnrollmentStatusV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EnrollmentStatusV1UpdateEndpointRequest, EnrollmentStatusV1UpdateCommand> requestMapper,
        [FromServices] IMapper<EnrollmentStatusV1UpdateCommandResult, EnrollmentStatusV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { EnrollmentStatusId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
