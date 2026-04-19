using School.MajorApi.Major.V1.SoftDelete.Command;

namespace School.MajorApi.Major.V1.SoftDelete.Endpoint;

[Route("/v1/school/majors/{id:guid}")]
[EndpointTag("School.Major")]
public sealed class MajorV1SoftDeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<MajorV1SoftDeleteCommand, MajorV1SoftDeleteEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization("AdminOnly");
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid id,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MajorV1SoftDeleteCommandResult, MajorV1SoftDeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MajorV1SoftDeleteCommand { MajorId = id };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
