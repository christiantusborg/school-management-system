namespace QuVian.SharedLibrary.Basics.Validations.Interfaces;


public interface IValidationRuleApplier<T>
{
    void Apply(AbstractValidator<T> validator, PropertyInfo property);
}

