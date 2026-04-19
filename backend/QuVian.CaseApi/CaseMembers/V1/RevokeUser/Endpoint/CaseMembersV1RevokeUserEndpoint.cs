using QuVian.CaseApi.CaseMembers.V1.RevokeUser.Command;

namespace QuVian.CaseApi.CaseMembers.V1.RevokeUser.Endpoint;

[Route("/v1/cases/{caseId:guid}/members/users/{targetUserId}")]
[EndpointTag("CaseMembers")]
public sealed class CaseMembersV1RevokeUserEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<CaseMembersV1RevokeUserCommand, CaseMembersV1RevokeUserEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId, string targetUserId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseMembersV1RevokeUserCommandResult, CaseMembersV1RevokeUserEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseMembersV1RevokeUserCommand
        {
            CaseId       = caseId,
            TargetUserId = targetUserId,
            ActorUserId  = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToResult(responseMapper);
    }
}
