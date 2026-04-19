namespace School.DocumentTypeApi.DocumentType.V1.PermanentDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1PermanentDeleteCommandValidator(
    ValidationRuleFactory<DocumentTypeV1PermanentDeleteCommand> validationRuleFactory)
    : BaseValidator<DocumentTypeV1PermanentDeleteCommand>(validationRuleFactory);
