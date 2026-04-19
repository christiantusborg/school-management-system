using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.GetPublicKey.Endpoint;

[Route("/v1/users/{userId}/kem-public-key")]
[EndpointTag("KemKeyPair")]
public sealed class KemKeyPairV1GetPublicKeyEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapGet<KemKeyPairV1GetPublicKeyCommand, KemKeyPairV1GetPublicKeyEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromRoute] string userId,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<KemKeyPairV1GetPublicKeyCommandResult, KemKeyPairV1GetPublicKeyEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new KemKeyPairV1GetPublicKeyCommand { UserId = userId };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
