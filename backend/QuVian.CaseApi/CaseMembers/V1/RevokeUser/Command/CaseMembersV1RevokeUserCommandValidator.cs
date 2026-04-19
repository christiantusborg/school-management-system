namespace QuVian.CaseApi.CaseMembers.V1.RevokeUser.Command;

public class CaseMembersV1RevokeUserCommandValidator : BaseValidator<CaseMembersV1RevokeUserCommand>
{
    public CaseMembersV1RevokeUserCommandValidator(ValidationRuleFactory<CaseMembersV1RevokeUserCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}
