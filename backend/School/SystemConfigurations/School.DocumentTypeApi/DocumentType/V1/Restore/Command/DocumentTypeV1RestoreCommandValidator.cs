namespace School.DocumentTypeApi.DocumentType.V1.Restore.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1RestoreCommandValidator(
    ValidationRuleFactory<DocumentTypeV1RestoreCommand> validationRuleFactory)
    : BaseValidator<DocumentTypeV1RestoreCommand>(validationRuleFactory);
