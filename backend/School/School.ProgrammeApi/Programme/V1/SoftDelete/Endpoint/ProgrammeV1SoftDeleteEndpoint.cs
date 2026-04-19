using School.ProgrammeApi.Programme.V1.SoftDelete.Command;

namespace School.ProgrammeApi.Programme.V1.SoftDelete.Endpoint;

[Route("/v1/school/programmes/{id:guid}")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<ProgrammeV1SoftDeleteCommand, ProgrammeV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ProgrammeV1SoftDeleteCommandResult, ProgrammeV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ProgrammeV1SoftDeleteCommand { ProgrammeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
