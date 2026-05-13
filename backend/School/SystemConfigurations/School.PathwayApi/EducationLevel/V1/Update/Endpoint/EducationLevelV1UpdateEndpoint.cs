using School.PathwayApi.EducationLevel.V1.Update.Command;

namespace School.PathwayApi.EducationLevel.V1.Update.Endpoint;

[Route("/v1/school/system-config/education-levels/{id:guid}")]
[EndpointTag("School.SystemConfig.EducationLevel")]
public sealed class EducationLevelV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<EducationLevelV1UpdateCommand, EducationLevelV1UpdateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromBody] EducationLevelV1UpdateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EducationLevelV1UpdateEndpointRequest, EducationLevelV1UpdateCommand> requestMapper,
        [FromServices] IMapper<EducationLevelV1UpdateCommandResult, EducationLevelV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { EducationLevelId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
