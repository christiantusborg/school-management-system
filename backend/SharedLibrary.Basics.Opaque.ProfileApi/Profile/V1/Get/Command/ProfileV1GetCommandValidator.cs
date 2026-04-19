namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProfileV1GetCommandValidator(
    ValidationRuleFactory<ProfileV1GetCommand> validationRuleFactory)
    : BaseValidator<ProfileV1GetCommand>(validationRuleFactory);
