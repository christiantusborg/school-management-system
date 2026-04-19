using School.SubjectApi.Subject.V1.PermanentDelete.Command;

namespace School.SubjectApi.Subject.V1.PermanentDelete.Endpoint;

[Route("/v1/school/subjects/{id:guid}/permanent")]
[EndpointTag("School.Subject")]
public sealed class SubjectV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<SubjectV1PermanentDeleteCommand, SubjectV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<SubjectV1PermanentDeleteCommandResult, SubjectV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new SubjectV1PermanentDeleteCommand { SubjectId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
