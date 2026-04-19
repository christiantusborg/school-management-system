using QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Command;

namespace QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Endpoint;

[Route("/v1/cases/{caseId:guid}/members/teams/{teamId:guid}")]
[EndpointTag("CaseMembers")]
public sealed class CaseMembersV1RevokeTeamEndpoint : IEndpointMarker
{
    public IEndpointRouteBuilder Map(IEndpointRouteBuilder app)
    {
        app.MapDelete<CaseMembersV1RevokeTeamCommand, CaseMembersV1RevokeTeamEndpointResponse>(this, EndpointHandlerAsync);
        return app;
    }

    private async Task<IResult> EndpointHandlerAsync(
        Guid caseId, Guid teamId,
        ClaimsPrincipal user,
        [FromServices] IDispatcher sender,
        [FromServices] IMapper<CaseMembersV1RevokeTeamCommandResult, CaseMembersV1RevokeTeamEndpointResponse> responseMapper,
        CancellationToken cancellationToken)
    {
        var command = new CaseMembersV1RevokeTeamCommand
        {
            CaseId      = caseId,
            TeamId      = teamId,
            ActorUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!
        };
        var result = await sender.SendAsync(command, cancellationToken).ConfigureAwait(false);
        return result.ToResult(responseMapper);
    }
}
