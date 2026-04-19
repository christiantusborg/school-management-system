namespace School.DocumentTypeApi.DocumentType.V1.SoftDelete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1SoftDeleteCommandValidator(
    ValidationRuleFactory<DocumentTypeV1SoftDeleteCommand> validationRuleFactory)
    : BaseValidator<DocumentTypeV1SoftDeleteCommand>(validationRuleFactory);
