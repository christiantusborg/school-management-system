using School.SubjectApi.Subject.V1.SoftDelete.Command;

namespace School.SubjectApi.Subject.V1.SoftDelete.Endpoint;

[Route("/v1/school/subjects/{id:guid}")]
[EndpointTag("School.Subject")]
public sealed class SubjectV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<SubjectV1SoftDeleteCommand, SubjectV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<SubjectV1SoftDeleteCommandResult, SubjectV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new SubjectV1SoftDeleteCommand { SubjectId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
