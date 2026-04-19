namespace QuVian.SharedLibrary.Validations;

public abstract class BaseValidator<T> : AbstractValidator<T>
{
    protected BaseValidator(ValidationRuleFactory<T> ruleFactory)
    {
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var rules = ruleFactory.GetRules(property);

            foreach (var rule in rules)
            {
                rule.Apply(this, property);
            }
        }
    }
}


