namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.Delete.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1DeleteCommandValidator(
    ValidationRuleFactory<PhonesV1DeleteCommand> validationRuleFactory)
    : BaseValidator<PhonesV1DeleteCommand>(validationRuleFactory);
