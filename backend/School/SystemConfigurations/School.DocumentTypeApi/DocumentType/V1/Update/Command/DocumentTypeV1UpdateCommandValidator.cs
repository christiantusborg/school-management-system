namespace School.DocumentTypeApi.DocumentType.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1UpdateCommandValidator(
    ValidationRuleFactory<DocumentTypeV1UpdateCommand> validationRuleFactory)
    : BaseValidator<DocumentTypeV1UpdateCommand>(validationRuleFactory);
