using School.MajorApi.Major.V1.Update.Command;

namespace School.MajorApi.Major.V1.Update.Endpoint;

[Route("/v1/school/majors/{id:guid}")]
[EndpointTag("School.Major")]
public sealed class MajorV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<MajorV1UpdateCommand, MajorV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] MajorV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MajorV1UpdateEndpointRequest, MajorV1UpdateCommand> requestMapper,
        [FromServices] IMapper<MajorV1UpdateCommandResult, MajorV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { MajorId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
