namespace QuVian.CaseApi.CaseMembers.V1.GrantTeam.Command;

public class CaseMembersV1GrantTeamCommandValidator : BaseValidator<CaseMembersV1GrantTeamCommand>
{
    public CaseMembersV1GrantTeamCommandValidator(ValidationRuleFactory<CaseMembersV1GrantTeamCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Level).InclusiveBetween(1, 6);
        RuleFor(x => x.TeamWrappedKeys).NotEmpty();
    }
}
