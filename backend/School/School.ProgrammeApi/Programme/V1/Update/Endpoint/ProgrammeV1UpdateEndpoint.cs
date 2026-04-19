using School.ProgrammeApi.Programme.V1.Update.Command;

namespace School.ProgrammeApi.Programme.V1.Update.Endpoint;

[Route("/v1/school/programmes/{id:guid}")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<ProgrammeV1UpdateCommand, ProgrammeV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] ProgrammeV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ProgrammeV1UpdateEndpointRequest, ProgrammeV1UpdateCommand> requestMapper,
        [FromServices] IMapper<ProgrammeV1UpdateCommandResult, ProgrammeV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { ProgrammeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
