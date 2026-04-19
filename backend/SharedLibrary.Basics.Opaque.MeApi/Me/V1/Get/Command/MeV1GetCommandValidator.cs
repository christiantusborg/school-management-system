namespace SharedLibrary.Basics.Opaque.MeApi.Me.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MeV1GetCommandValidator(
    ValidationRuleFactory<MeV1GetCommand> validationRuleFactory)
    : BaseValidator<MeV1GetCommand>(validationRuleFactory);
