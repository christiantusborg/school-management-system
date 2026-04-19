using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuVian.SharedLibrary.Validations.Interfaces;
using QuVian.SharedLibrary.Validations.RuleProviders.AllowJavaScriptRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.AnyDateRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.EmailAddressRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.FutureDateRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.GreaterThanRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.LessThanRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.MaxLengthRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.MinLengthLengthRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.PastDateRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.RangeRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.RegularExpressionRuleProviders;
using QuVian.SharedLibrary.Validations.RuleProviders.UrlRuleProviders;

namespace QuVian.SharedLibrary.Validations;

public static class ValidationsServicesExtension
{
    /// <summary>
    /// Master method that supports both manual registration and assembly scanning.
    /// </summary>
    public static IServiceCollection AddValidations(
        this IServiceCollection services,
        Action<IServiceCollection>? manual = null,
        Assembly[]? assemblies = null)
    {
        // Apply manual registrations
        manual?.Invoke(services);

        // Scan assemblies for validation providers
        if (assemblies is { Length: > 0 })
        {
            foreach (var assembly in assemblies)
            {
                var providers = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract)
                    .SelectMany(t => t.GetInterfaces()
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationRuleProvider<>))
                        .Select(i => new
                        {
                            Service = i.GetGenericTypeDefinition(),
                            Impl = t.GetGenericTypeDefinition()
                        }))
                    .Distinct();

                foreach (var pair in providers)
                {
                    var descriptor = ServiceDescriptor.Transient(pair.Service, pair.Impl);

                    if (services.Any(s =>
                            s.ServiceType == descriptor.ServiceType &&
                            s.ImplementationType == descriptor.ImplementationType))
                    {
                        Console.WriteLine($"⚠️ Skipped duplicate registration: {pair.Service.Name} -> {pair.Impl.Name}");
                        continue;
                    }

                    services.Add(descriptor);
                }
            }
        }

        // Always register the factory once
        services.TryAddTransient(typeof(ValidationRuleFactory<>));

        return services;
    }

    /// <summary>
    /// Overload 1: No manual registrations, no scanning.
    /// </summary>
    public static IServiceCollection AddValidations(this IServiceCollection services)
    {
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(AllowJavaScriptValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(RegularExpressionValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(MaxLengthValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(MinLengthLengthValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(RangeValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(AnyDateValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(LessThanValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(GreaterThanValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(PastDateValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(FutureDateValidationRuleProvider<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(EmailAddressValidationRuleApplier<>));
        services.TryAddTransient(typeof(IValidationRuleProvider<>), typeof(UrlValidationRuleProvider<>));
        return services;
    }


    /// <summary>
    /// Overload 2: Assembly scanning only.
    /// </summary>
    public static IServiceCollection AddValidations(this IServiceCollection services, Assembly[] assemblies)
        => services.AddValidations(null, assemblies);

    /// <summary>
    /// Overload 3: Manual registrations only.
    /// </summary>
    public static IServiceCollection AddValidations(this IServiceCollection services, Action<IServiceCollection> manual)
        => services.AddValidations(manual, null);
}
