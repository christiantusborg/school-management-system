namespace School.DocumentTypeApi.DocumentType.V1.Create.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1CreateCommandValidator(
    ValidationRuleFactory<DocumentTypeV1CreateCommand> validationRuleFactory)
    : BaseValidator<DocumentTypeV1CreateCommand>(validationRuleFactory);
