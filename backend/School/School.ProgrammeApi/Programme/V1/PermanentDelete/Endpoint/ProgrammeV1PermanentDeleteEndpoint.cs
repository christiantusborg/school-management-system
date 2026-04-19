using School.ProgrammeApi.Programme.V1.PermanentDelete.Command;

namespace School.ProgrammeApi.Programme.V1.PermanentDelete.Endpoint;

[Route("/v1/school/programmes/{id:guid}/permanent")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<ProgrammeV1PermanentDeleteCommand, ProgrammeV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ProgrammeV1PermanentDeleteCommandResult, ProgrammeV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ProgrammeV1PermanentDeleteCommand { ProgrammeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
