using School.MajorApi.Major.V1.Get.Command;

namespace School.MajorApi.Major.V1.Get.Endpoint;

[Route("/v1/school/majors/{id:guid}")]
[EndpointTag("School.Major")]
public sealed class MajorV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<MajorV1GetCommand, MajorV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MajorV1GetCommandResult, MajorV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MajorV1GetCommand { MajorId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
