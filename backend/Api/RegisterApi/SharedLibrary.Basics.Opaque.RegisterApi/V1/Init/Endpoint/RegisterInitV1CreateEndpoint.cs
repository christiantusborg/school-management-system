using QuVian.SharedLibrary.Basics.SuccessOrFailures;
using SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Command;

namespace SharedLibrary.Basics.Opaque.RegisterApi.V1.Init.Endpoint;

[Route("/v1/register/init")]
[EndpointTag("RegisterInit")]
public sealed class RegisterInitV1CreateEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapPost<RegisterInitV1CreateCommand, RegisterInitV1CreateEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync([FromBody] RegisterInitV1CreateEndpointRequest request,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<RegisterInitV1CreateEndpointRequest, RegisterInitV1CreateCommand> requestMapper,
        [FromServices]
        IMapper<RegisterInitV1CreateCommandResult, RegisterInitV1CreateEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = requestMapper.MapFrom(request);

        SuccessOrFailure<RegisterInitV1CreateCommandResult>? commandResult = null;
        try
        {
            commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        //var commandResult = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);

        var result = commandResult.ToCreatedResult(responseMapper);
        return result;
    }
}