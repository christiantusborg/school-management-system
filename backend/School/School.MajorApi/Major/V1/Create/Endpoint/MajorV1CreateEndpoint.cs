using School.MajorApi.Major.V1.Create.Command;
using School.MajorApi.Major.V1.Create.Endpoint.Mappers;

namespace School.MajorApi.Major.V1.Create.Endpoint;

[Route("/v1/school/majors")]
[EndpointTag("School.Major")]
public sealed class MajorV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MajorV1CreateCommand, MajorV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] MajorV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MajorV1CreateEndpointRequest, MajorV1CreateCommand> requestMapper,
        [FromServices] IMapper<MajorV1CreateCommandResult, MajorV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
