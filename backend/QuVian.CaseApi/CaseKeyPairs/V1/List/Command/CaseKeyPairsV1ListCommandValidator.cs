namespace QuVian.CaseApi.CaseKeyPairs.V1.List.Command;

public class CaseKeyPairsV1ListCommandValidator : BaseValidator<CaseKeyPairsV1ListCommand>
{
    public CaseKeyPairsV1ListCommandValidator(ValidationRuleFactory<CaseKeyPairsV1ListCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
    }
}
