namespace School.DocumentTypeApi.DocumentType.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class DocumentTypeV1GetCommandValidator(
    ValidationRuleFactory<DocumentTypeV1GetCommand> validationRuleFactory)
    : BaseValidator<DocumentTypeV1GetCommand>(validationRuleFactory);
