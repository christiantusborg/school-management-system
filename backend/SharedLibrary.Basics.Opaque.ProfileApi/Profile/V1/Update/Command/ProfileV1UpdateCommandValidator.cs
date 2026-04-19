namespace SharedLibrary.Basics.Opaque.ProfileApi.Profile.V1.Update.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class ProfileV1UpdateCommandValidator(
    ValidationRuleFactory<ProfileV1UpdateCommand> validationRuleFactory)
    : BaseValidator<ProfileV1UpdateCommand>(validationRuleFactory);
