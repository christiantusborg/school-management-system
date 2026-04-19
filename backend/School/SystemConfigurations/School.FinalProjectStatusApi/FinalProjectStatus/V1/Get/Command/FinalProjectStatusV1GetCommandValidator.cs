namespace School.FinalProjectStatusApi.FinalProjectStatus.V1.Get.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class FinalProjectStatusV1GetCommandValidator(
    ValidationRuleFactory<FinalProjectStatusV1GetCommand> validationRuleFactory)
    : BaseValidator<FinalProjectStatusV1GetCommand>(validationRuleFactory);
