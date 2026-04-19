using School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Command;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.PermanentDelete.Endpoint;

[Route("/v1/school/system-config/modes-of-study/{id:int}/permanent")]
[EndpointTag("School.SystemConfig.ModeOfStudy")]
public sealed class ModeOfStudyV1PermanentDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<ModeOfStudyV1PermanentDeleteCommand, ModeOfStudyV1PermanentDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ModeOfStudyV1PermanentDeleteCommandResult, ModeOfStudyV1PermanentDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ModeOfStudyV1PermanentDeleteCommand { ModeOfStudyId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
