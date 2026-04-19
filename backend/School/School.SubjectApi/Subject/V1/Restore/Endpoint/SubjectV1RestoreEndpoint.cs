using School.SubjectApi.Subject.V1.Restore.Command;

namespace School.SubjectApi.Subject.V1.Restore.Endpoint;

[Route("/v1/school/subjects/{id:guid}/restore")]
[EndpointTag("School.Subject")]
public sealed class SubjectV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<SubjectV1RestoreCommand, SubjectV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<SubjectV1RestoreCommandResult, SubjectV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new SubjectV1RestoreCommand { SubjectId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
