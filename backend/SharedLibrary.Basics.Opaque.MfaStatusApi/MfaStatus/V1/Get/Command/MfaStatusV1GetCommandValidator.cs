namespace SharedLibrary.Basics.Opaque.MfaStatusApi.MfaStatus.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class MfaStatusV1GetCommandValidator(
    ValidationRuleFactory<MfaStatusV1GetCommand> validationRuleFactory)
    : BaseValidator<MfaStatusV1GetCommand>(validationRuleFactory);
