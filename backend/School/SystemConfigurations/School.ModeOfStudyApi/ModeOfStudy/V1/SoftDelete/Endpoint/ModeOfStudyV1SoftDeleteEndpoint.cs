using School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Command;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.SoftDelete.Endpoint;

[Route("/v1/school/system-config/modes-of-study/{id:int}")]
[EndpointTag("School.SystemConfig.ModeOfStudy")]
public sealed class ModeOfStudyV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<ModeOfStudyV1SoftDeleteCommand, ModeOfStudyV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ModeOfStudyV1SoftDeleteCommandResult, ModeOfStudyV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ModeOfStudyV1SoftDeleteCommand { ModeOfStudyId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
