namespace QuVian.CaseApi.CaseMembers.V1.RevokeTeam.Command;

public class CaseMembersV1RevokeTeamCommandValidator : BaseValidator<CaseMembersV1RevokeTeamCommand>
{
    public CaseMembersV1RevokeTeamCommandValidator(ValidationRuleFactory<CaseMembersV1RevokeTeamCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
