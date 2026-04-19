namespace QuVian.CaseApi.CaseMembers.V1.GrantUser.Command;

public class CaseMembersV1GrantUserCommandValidator : BaseValidator<CaseMembersV1GrantUserCommand>
{
    public CaseMembersV1GrantUserCommandValidator(ValidationRuleFactory<CaseMembersV1GrantUserCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Level).InclusiveBetween(1, 6);
        RuleFor(x => x.WrappedKeys).NotEmpty();
    }
}
