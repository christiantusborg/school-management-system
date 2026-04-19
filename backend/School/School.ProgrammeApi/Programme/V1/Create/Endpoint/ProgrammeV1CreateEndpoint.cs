using School.ProgrammeApi.Programme.V1.Create.Command;
using School.ProgrammeApi.Programme.V1.Create.Endpoint.Mappers;

namespace School.ProgrammeApi.Programme.V1.Create.Endpoint;

[Route("/v1/school/programmes")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<ProgrammeV1CreateCommand, ProgrammeV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] ProgrammeV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ProgrammeV1CreateEndpointRequest, ProgrammeV1CreateCommand> requestMapper,
        [FromServices] IMapper<ProgrammeV1CreateCommandResult, ProgrammeV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
