using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.SetPrimary.Endpoint;

[Route("/v1/profile/addresses/{userAddressId:guid}/set-primary")]
[EndpointTag("Addresses")]
public sealed class AddressesV1SetPrimaryEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<AddressesV1SetPrimaryCommand, AddressesV1SetPrimaryEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userAddressId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AddressesV1SetPrimaryCommandResult, AddressesV1SetPrimaryEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new AddressesV1SetPrimaryCommand
        {
            UserAddressId = userAddressId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
