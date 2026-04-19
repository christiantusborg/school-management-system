using School.ModeOfStudyApi.ModeOfStudy.V1.Create.Command;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Create.Endpoint;

[Route("/v1/school/system-config/modes-of-study")]
[EndpointTag("School.SystemConfig.ModeOfStudy")]
public sealed class ModeOfStudyV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<ModeOfStudyV1CreateCommand, ModeOfStudyV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] ModeOfStudyV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ModeOfStudyV1CreateEndpointRequest, ModeOfStudyV1CreateCommand> requestMapper,
        [FromServices] IMapper<ModeOfStudyV1CreateCommandResult, ModeOfStudyV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
