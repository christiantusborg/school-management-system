namespace QuVian.CaseApi.CaseMembers.V1.List.Command;

public class CaseMembersV1ListCommandValidator : BaseValidator<CaseMembersV1ListCommand>
{
    public CaseMembersV1ListCommandValidator(ValidationRuleFactory<CaseMembersV1ListCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
    }
}
