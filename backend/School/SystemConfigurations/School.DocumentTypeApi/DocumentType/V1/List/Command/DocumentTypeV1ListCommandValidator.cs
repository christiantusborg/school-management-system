namespace School.DocumentTypeApi.DocumentType.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1ListCommandValidator(
    ValidationRuleFactory<DocumentTypeV1ListCommand> validationRuleFactory)
    : BaseValidator<DocumentTypeV1ListCommand>(validationRuleFactory);
