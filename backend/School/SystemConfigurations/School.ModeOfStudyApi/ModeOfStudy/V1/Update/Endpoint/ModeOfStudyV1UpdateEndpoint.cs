using School.ModeOfStudyApi.ModeOfStudy.V1.Update.Command;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Update.Endpoint;

[Route("/v1/school/system-config/modes-of-study/{id:int}")]
[EndpointTag("School.SystemConfig.ModeOfStudy")]
public sealed class ModeOfStudyV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<ModeOfStudyV1UpdateCommand, ModeOfStudyV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromBody] ModeOfStudyV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ModeOfStudyV1UpdateEndpointRequest, ModeOfStudyV1UpdateCommand> requestMapper,
        [FromServices] IMapper<ModeOfStudyV1UpdateCommandResult, ModeOfStudyV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { ModeOfStudyId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
