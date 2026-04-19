namespace QuVian.CaseApi.CaseUserKeys.V1.GetMy.Command;

public class CaseUserKeysV1GetMyCommandValidator : BaseValidator<CaseUserKeysV1GetMyCommand>
{
    public CaseUserKeysV1GetMyCommandValidator(ValidationRuleFactory<CaseUserKeysV1GetMyCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
