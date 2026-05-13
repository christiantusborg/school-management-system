using School.PathwayApi.EducationLevel.V1.SoftDelete.Command;

namespace School.PathwayApi.EducationLevel.V1.SoftDelete.Endpoint;

[Route("/v1/school/system-config/education-levels/{id:guid}")]
[EndpointTag("School.SystemConfig.EducationLevel")]
public sealed class EducationLevelV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<EducationLevelV1SoftDeleteCommand, EducationLevelV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EducationLevelV1SoftDeleteCommandResult, EducationLevelV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new EducationLevelV1SoftDeleteCommand { EducationLevelId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
