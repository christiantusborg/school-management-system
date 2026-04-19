using SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Command;

namespace SharedLibrary.Basics.Opaque.ProfileApi.Emails.V1.Create.Endpoint;

[Route("/v1/profile/emails")]
[EndpointTag("Emails")]
public sealed class EmailsV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<EmailsV1CreateCommand, EmailsV1CreateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        [FromBody] EmailsV1CreateEndpointRequest request,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<EmailsV1CreateEndpointRequest, EmailsV1CreateCommand> requestMapper,
        [FromServices] IMapper<EmailsV1CreateCommandResult, EmailsV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request) with { UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)! };
        var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return commandResult.ToCreatedResult(responseMapper);
    }
}
