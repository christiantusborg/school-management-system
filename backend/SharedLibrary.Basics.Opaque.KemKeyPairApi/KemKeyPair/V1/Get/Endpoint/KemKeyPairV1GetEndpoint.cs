using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Get.Endpoint;

[Route("/v1/account/kem-keypair")]
[EndpointTag("KemKeyPair")]
public sealed class KemKeyPairV1GetEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<KemKeyPairV1GetCommand, KemKeyPairV1GetEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<KemKeyPairV1GetCommandResult, KemKeyPairV1GetEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new KemKeyPairV1GetCommand { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
