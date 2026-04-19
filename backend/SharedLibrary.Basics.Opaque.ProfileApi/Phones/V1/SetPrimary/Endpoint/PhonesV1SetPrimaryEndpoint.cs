using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.SetPrimary.Endpoint;

[Route("/v1/profile/phones/{userPhoneId:guid}/set-primary")]
[EndpointTag("Phones")]
public sealed class PhonesV1SetPrimaryEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<PhonesV1SetPrimaryCommand, PhonesV1SetPrimaryEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userPhoneId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PhonesV1SetPrimaryCommandResult, PhonesV1SetPrimaryEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PhonesV1SetPrimaryCommand
        {
            UserPhoneId = userPhoneId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
