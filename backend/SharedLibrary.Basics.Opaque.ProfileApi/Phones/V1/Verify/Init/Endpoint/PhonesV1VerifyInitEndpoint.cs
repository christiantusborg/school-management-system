using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Init.Endpoint;

[Route("/v1/profile/phones/{userPhoneId:guid}/verify/init")]
[EndpointTag("Phones")]
public sealed class PhonesV1VerifyInitEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<PhonesV1VerifyInitCommand, PhonesV1VerifyInitEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromRoute] Guid userPhoneId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PhonesV1VerifyInitCommandResult, PhonesV1VerifyInitEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new PhonesV1VerifyInitCommand
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            UserPhoneId = userPhoneId
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
