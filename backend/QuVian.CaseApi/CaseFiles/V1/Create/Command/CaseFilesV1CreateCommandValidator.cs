namespace QuVian.CaseApi.CaseFiles.V1.Create.Command;

public class CaseFilesV1CreateCommandValidator : BaseValidator<CaseFilesV1CreateCommand>
{
    public CaseFilesV1CreateCommandValidator(ValidationRuleFactory<CaseFilesV1CreateCommand> factory) : base(factory)
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.StoragePath).NotEmpty();
        RuleFor(x => x.MinLevel).InclusiveBetween(1, 6);
        RuleFor(x => x.LevelKeys).NotEmpty();
    }
}
