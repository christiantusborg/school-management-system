namespace QuVian.SharedLibrary.Basics.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="Type"/> class.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Retrieves the first property of a given type that has the specified attribute.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
    /// <param name="type">The type to search for the property.</param>
    /// <returns>The first property that has the specified attribute, or <c>null</c> if no such property is found.</returns>
    public static PropertyInfo? GetPropertyWithAttribute<TAttribute>(this Type type)
        where TAttribute : Attribute
    {
        ArgumentNullException.ThrowIfNull(type);
        return type.GetProperties()
            .FirstOrDefault(property => property.IsDefined(typeof(TAttribute), inherit: true));
    }
}
