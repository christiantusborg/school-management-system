using School.ModeOfStudyApi.ModeOfStudy.V1.Get.Command;

namespace School.ModeOfStudyApi.ModeOfStudy.V1.Get.Endpoint;

[Route("/v1/school/system-config/modes-of-study/{id:int}")]
[EndpointTag("School.SystemConfig.ModeOfStudy")]
public sealed class ModeOfStudyV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<ModeOfStudyV1GetCommand, ModeOfStudyV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        int id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ModeOfStudyV1GetCommandResult, ModeOfStudyV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ModeOfStudyV1GetCommand { ModeOfStudyId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
