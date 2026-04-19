using SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Endpoint;

[Route("/v1/me")]
[EndpointTag("Me")]
public sealed class MeV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<MeV1GetCommand, MeV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<MeV1GetCommandResult, MeV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new MeV1GetCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
