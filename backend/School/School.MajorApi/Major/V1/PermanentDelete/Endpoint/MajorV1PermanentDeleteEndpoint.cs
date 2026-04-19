using School.MajorApi.Major.V1.PermanentDelete.Command;

namespace School.MajorApi.Major.V1.PermanentDelete.Endpoint;

[Route("/v1/school/majors/{id:guid}/permanent")]
[EndpointTag("School.Major")]
public sealed class MajorV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<MajorV1PermanentDeleteCommand, MajorV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MajorV1PermanentDeleteCommandResult, MajorV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MajorV1PermanentDeleteCommand { MajorId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
