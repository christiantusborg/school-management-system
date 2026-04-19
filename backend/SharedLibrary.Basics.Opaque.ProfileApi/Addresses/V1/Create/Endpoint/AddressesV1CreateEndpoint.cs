using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Create.Endpoint;

[Route("/v1/profile/addresses")]
[EndpointTag("Addresses")]
public sealed class AddressesV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<AddressesV1CreateCommand, AddressesV1CreateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] AddressesV1CreateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AddressesV1CreateEndpointRequest, AddressesV1CreateCommand> requestMapper,
        [FromServices] IMapper<AddressesV1CreateCommandResult, AddressesV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
