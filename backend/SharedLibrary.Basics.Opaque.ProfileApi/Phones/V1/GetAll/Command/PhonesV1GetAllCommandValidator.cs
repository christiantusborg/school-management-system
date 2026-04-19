namespace SharedLibrary.Basics.Opaque.ProfileApi.Phones.V1.GetAll.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class PhonesV1GetAllCommandValidator(
    ValidationRuleFactory<PhonesV1GetAllCommand> validationRuleFactory)
    : BaseValidator<PhonesV1GetAllCommand>(validationRuleFactory);
