using School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Command;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Restore.Endpoint;

[Route("/v1/school/system-config/modes-of-study/{id:int}/restore")]
[EndpointTag("School.SystemConfig.ModeOfStudy")]
public sealed class ModeOfStudyV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<ModeOfStudyV1RestoreCommand, ModeOfStudyV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ModeOfStudyV1RestoreCommandResult, ModeOfStudyV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ModeOfStudyV1RestoreCommand { ModeOfStudyId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
