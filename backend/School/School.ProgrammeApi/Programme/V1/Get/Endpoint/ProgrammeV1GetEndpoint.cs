using School.ProgrammeApi.Programme.V1.Get.Command;

namespace School.ProgrammeApi.Programme.V1.Get.Endpoint;

[Route("/v1/school/programmes/{id:guid}")]
[EndpointTag("School.Programme")]
public sealed class ProgrammeV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<ProgrammeV1GetCommand, ProgrammeV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<ProgrammeV1GetCommandResult, ProgrammeV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new ProgrammeV1GetCommand { ProgrammeId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
