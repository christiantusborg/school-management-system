using SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Addresses.V1.Update.Endpoint;

[Route("/v1/profile/addresses/{userAddressId:guid}")]
[EndpointTag("Addresses")]
public sealed class AddressesV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<AddressesV1UpdateCommand, AddressesV1UpdateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userAddressId,
        [FromBody] AddressesV1UpdateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<AddressesV1UpdateEndpointRequest, AddressesV1UpdateCommand> requestMapper,
        [FromServices] IMapper<AddressesV1UpdateCommandResult, AddressesV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with
        {
            UserAddressId = userAddressId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
