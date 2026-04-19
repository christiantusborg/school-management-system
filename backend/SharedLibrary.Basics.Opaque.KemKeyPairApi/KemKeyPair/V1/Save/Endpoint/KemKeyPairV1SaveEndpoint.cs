using SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Command;

namespace SharedLibrary.Basics.Opaque.KemKeyPairApi.KemKeyPair.V1.Save.Endpoint;

[Route("/v1/account/kem-keypair")]
[EndpointTag("KemKeyPair")]
public sealed class KemKeyPairV1SaveEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<KemKeyPairV1SaveCommand, KemKeyPairV1SaveEndpointResponse>(this, EndpointHandlerAsync)
            .RequireAuthorization();
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] KemKeyPairV1SaveEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<KemKeyPairV1SaveEndpointRequest, KemKeyPairV1SaveCommand> requestMapper,
        [FromServices] IMapper<KemKeyPairV1SaveCommandResult, KemKeyPairV1SaveEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
