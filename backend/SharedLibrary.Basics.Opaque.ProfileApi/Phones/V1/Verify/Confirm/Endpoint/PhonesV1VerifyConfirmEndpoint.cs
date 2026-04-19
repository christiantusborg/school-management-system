using SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Verify.Confirm.Endpoint;

[Route("/v1/profile/phones/verify/confirm")]
[EndpointTag("Phones")]
public sealed class PhonesV1VerifyConfirmEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<PhonesV1VerifyConfirmCommand, PhonesV1VerifyConfirmEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] PhonesV1VerifyConfirmEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<PhonesV1VerifyConfirmEndpointRequest, PhonesV1VerifyConfirmCommand> requestMapper,
        [FromServices] IMapper<PhonesV1VerifyConfirmCommandResult, PhonesV1VerifyConfirmEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
