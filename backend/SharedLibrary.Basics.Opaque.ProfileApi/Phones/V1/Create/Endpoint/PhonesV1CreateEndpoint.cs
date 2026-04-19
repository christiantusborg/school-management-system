using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Create.Endpoint;

[Route("/v1/profile/phones")]
[EndpointTag("Phones")]
public sealed class PhonesV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<PhonesV1CreateCommand, PhonesV1CreateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] PhonesV1CreateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PhonesV1CreateEndpointRequest, PhonesV1CreateCommand> requestMapper,
        [FromServices] IMapper<PhonesV1CreateCommandResult, PhonesV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
