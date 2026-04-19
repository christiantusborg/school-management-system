using School.ProgrammeApi.Programme.V1.Restore.Command;

namespace School.ProgrammeApi.Programme.V1.Restore.Endpoint;

[Route("/v1/school/programmes/{id:guid}/restore")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<ProgrammeV1RestoreCommand, ProgrammeV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ProgrammeV1RestoreCommandResult, ProgrammeV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ProgrammeV1RestoreCommand { ProgrammeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
