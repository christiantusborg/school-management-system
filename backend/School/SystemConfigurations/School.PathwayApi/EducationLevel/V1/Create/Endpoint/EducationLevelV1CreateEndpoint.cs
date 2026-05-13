using School.PathwayApi.EducationLevel.V1.Create.Command;

namespace School.PathwayApi.EducationLevel.V1.Create.Endpoint;

[Route("/v1/school/system-config/education-levels")]
[EndpointTag("School.SystemConfig.EducationLevel")]
public sealed class EducationLevelV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<EducationLevelV1CreateCommand, EducationLevelV1CreateEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] EducationLevelV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EducationLevelV1CreateEndpointRequest, EducationLevelV1CreateCommand> requestMapper,
        [FromServices] IMapper<EducationLevelV1CreateCommandResult, EducationLevelV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
