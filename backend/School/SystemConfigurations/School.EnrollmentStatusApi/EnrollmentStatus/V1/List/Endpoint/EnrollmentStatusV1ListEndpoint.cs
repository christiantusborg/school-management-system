using School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Command;

namespace School.EnrollmentStatusApi.EnrollmentStatus.V1.List.Endpoint;

[Route("/v1/school/system-config/enrollment-statuses")]
[EndpointTag("School.SystemConfig.EnrollmentStatus")]
public sealed class EnrollmentStatusV1ListEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<EnrollmentStatusV1ListCommand, BaseGetAllResponse<EnrollmentStatusV1ListEndpointResponseItem>>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CommandSearchResult<EnrollmentStatusV1ListCommandResultItem>, BaseGetAllResponse<EnrollmentStatusV1ListEndpointResponseItem>> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EnrollmentStatusV1ListCommand();
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
