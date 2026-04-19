using School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Command;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.Create.Endpoint;

[Route("/v1/school/system-config/enrollment-statuses")]
[EndpointTag("School.SystemConfig.EnrollmentStatus")]
public sealed class EnrollmentStatusV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<EnrollmentStatusV1CreateCommand, EnrollmentStatusV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] EnrollmentStatusV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EnrollmentStatusV1CreateEndpointRequest, EnrollmentStatusV1CreateCommand> requestMapper,
        [FromServices] IMapper<EnrollmentStatusV1CreateCommandResult, EnrollmentStatusV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
