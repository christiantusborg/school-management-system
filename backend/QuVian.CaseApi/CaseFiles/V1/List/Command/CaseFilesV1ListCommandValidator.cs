namespace QuVian.CaseApi.CaseFiles.V1.List.Command;

public class CaseFilesV1ListCommandValidator : BaseValidator<CaseFilesV1ListCommand>
{
    public CaseFilesV1ListCommandValidator(ValidationRuleFactory<CaseFilesV1ListCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
