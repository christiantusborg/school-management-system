using QuVian.SharedLibrary.Basics.Validations.Interfaces;

namespace QuVian.SharedLibrary.Validations.Interfaces;

public interface IValidationRuleProvider<T>
{
    bool CanHandle(PropertyInfo property);
    IValidationRuleApplier<T> CreateRule(PropertyInfo property);
}

// Implement other providers as needed
