using School.MajorApi.Major.V1.Restore.Command;

namespace School.MajorApi.Major.V1.Restore.Endpoint;

[Route("/v1/school/majors/{id:guid}/restore")]
[EndpointTag("School.Major")]
public sealed class MajorV1RestoreEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<MajorV1RestoreCommand, MajorV1RestoreEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MajorV1RestoreCommandResult, MajorV1RestoreEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MajorV1RestoreCommand { MajorId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
