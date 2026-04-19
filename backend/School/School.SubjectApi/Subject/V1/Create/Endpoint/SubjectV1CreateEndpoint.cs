using School.SubjectApi.Subject.V1.Create.Command;
using School.SubjectApi.Subject.V1.Create.Endpoint.Mappers;

namespace School.SubjectApi.Subject.V1.Create.Endpoint;

[Route("/v1/school/subjects")]
[EndpointTag("School.Subject")]
public sealed class SubjectV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<SubjectV1CreateCommand, SubjectV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] SubjectV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<SubjectV1CreateEndpointRequest, SubjectV1CreateCommand> requestMapper,
        [FromServices] IMapper<SubjectV1CreateCommandResult, SubjectV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
