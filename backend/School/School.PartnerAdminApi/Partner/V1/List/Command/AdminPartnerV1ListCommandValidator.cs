namespace School.PartnerAdminApi.Partner.V1.List.Command;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public sealed class AdminPartnerV1ListCommandValidator(ValidationRuleFactory<AdminPartnerV1ListCommand> f)
    : BaseValidator<AdminPartnerV1ListCommand>(f);
