using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Delete.Endpoint;

[Route("/v1/profile/addresses/{userAddressId:guid}")]
[EndpointTag("Addresses")]
public sealed class AddressesV1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<AddressesV1DeleteCommand, AddressesV1DeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userAddressId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AddressesV1DeleteCommandResult, AddressesV1DeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AddressesV1DeleteCommand
        {
            UserAddressId = userAddressId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
