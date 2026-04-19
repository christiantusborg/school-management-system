namespace QuVian.CaseApi.CaseFiles.V1.GetKey.Command;

public class CaseFilesV1GetKeyCommandValidator : BaseValidator<CaseFilesV1GetKeyCommand>
{
    public CaseFilesV1GetKeyCommandValidator(ValidationRuleFactory<CaseFilesV1GetKeyCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.CaseFileId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
