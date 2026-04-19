using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Endpoint;

[Route("/v1/profile/phones/{userPhoneId:guid}")]
[EndpointTag("Phones")]
public sealed class PhonesV1DeleteEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<PhonesV1DeleteCommand, PhonesV1DeleteEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userPhoneId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PhonesV1DeleteCommandResult, PhonesV1DeleteEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PhonesV1DeleteCommand
        {
            UserPhoneId = userPhoneId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
