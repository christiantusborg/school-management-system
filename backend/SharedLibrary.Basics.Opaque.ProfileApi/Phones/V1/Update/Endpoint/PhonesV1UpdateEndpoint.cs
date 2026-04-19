using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Update.Endpoint;

[Route("/v1/profile/phones/{userPhoneId:guid}")]
[EndpointTag("Phones")]
public sealed class PhonesV1UpdateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPut<PhonesV1UpdateCommand, PhonesV1UpdateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid userPhoneId,
        [FromBody] PhonesV1UpdateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PhonesV1UpdateEndpointRequest, PhonesV1UpdateCommand> requestMapper,
        [FromServices] IMapper<PhonesV1UpdateCommandResult, PhonesV1UpdateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with
        {
            UserPhoneId = userPhoneId,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToResult(responseMapper);
    }
}
