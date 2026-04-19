using School.SubjectApi.Subject.V1.Update.Command;

namespace School.SubjectApi.Subject.V1.Update.Endpoint;

[Route("/v1/school/subjects/{id:guid}")]
[EndpointTag("School.Subject")]
public sealed class SubjectV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<SubjectV1UpdateCommand, SubjectV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] SubjectV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<SubjectV1UpdateEndpointRequest, SubjectV1UpdateCommand> requestMapper,
        [FromServices] IMapper<SubjectV1UpdateCommandResult, SubjectV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { SubjectId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
