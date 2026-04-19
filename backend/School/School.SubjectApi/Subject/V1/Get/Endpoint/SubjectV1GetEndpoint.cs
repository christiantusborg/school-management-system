using School.SubjectApi.Subject.V1.Get.Command;

namespace School.SubjectApi.Subject.V1.Get.Endpoint;

[Route("/v1/school/subjects/{id:guid}")]
[EndpointTag("School.Subject")]
public sealed class SubjectV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<SubjectV1GetCommand, SubjectV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<SubjectV1GetCommandResult, SubjectV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new SubjectV1GetCommand { SubjectId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
