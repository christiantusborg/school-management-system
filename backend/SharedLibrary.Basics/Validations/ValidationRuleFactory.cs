using QuVian.SharedLibrary.Basics.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations;

public class ValidationRuleFactory<T>
{
    private readonly IEnumerable<IValidationRuleProvider<T>> _ruleProviders;

    public ValidationRuleFactory(IEnumerable<IValidationRuleProvider<T>> ruleProviders)
    {
        _ruleProviders = ruleProviders;
    }

    public IEnumerable<IValidationRuleApplier<T>> GetRules(PropertyInfo property)
    {

        return _ruleProviders
            .Where(provider => provider.CanHandle(property))
            .Select(provider => provider.CreateRule(property));
    }
}
