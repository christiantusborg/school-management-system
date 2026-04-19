using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;
using QuVian.SharedLibrary.Basics.Filters;

namespace QuVian.SharedLibrary.Basics.Repositories.Specifications;

[SuppressMessage("Style", "IDE1006:Naming Styles")]
public static class WhereBuilderExtension
{
    private static readonly MethodInfo ContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!
        ?? throw new MissingMethodException("string.Contains(string)");

    private static readonly MethodInfo StartsWithMethod =
        typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!
        ?? throw new MissingMethodException("string.StartsWith(string)");

    private static readonly MethodInfo EndsWithMethod =
        typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) })!
        ?? throw new MissingMethodException("string.EndsWith(string)");

    /// <summary>
    /// Builds a combined WHERE expression from flat filters and filter groups.
    /// Flat filters are evaluated first as one implicit AND block, then each
    /// group is appended using its own <see cref="FilterGroup.GroupOperator"/>.
    /// </summary>
    public static Expression<Func<T, bool>>? GetExpression<T>(
        IReadOnlyList<Filter> filters,
        IReadOnlyList<FilterGroup> groups)
    {
        Debug.Assert(filters is not null);
        Debug.Assert(groups is not null);

        if (filters.Count == 0 && groups.Count == 0) return null;

        var param = Expression.Parameter(typeof(T), "t");
        Expression? body = null;

        // flat filters → implicit first group
        if (filters.Count > 0)
            body = BuildGroupBody(param, filters);

        // named groups
        foreach (var group in groups)
        {
            var groupBody = BuildGroupBody(param, group.Filters);
            if (groupBody is null) continue;

            body = body is null
                ? groupBody
                : group.GroupOperator == LogicalOperator.Or
                    ? Expression.OrElse(body, groupBody)
                    : Expression.AndAlso(body, groupBody);
        }

        return body is null ? null : Expression.Lambda<Func<T, bool>>(body, param);
    }

    /// <summary>Convenience overload for flat filters only (no groups).</summary>
    public static Expression<Func<T, bool>>? GetExpression<T>(IReadOnlyList<Filter> filters)
        => GetExpression<T>(filters, Array.Empty<FilterGroup>());

    // ---------- helpers -----------------------------------------------------

    private static Expression? BuildGroupBody(ParameterExpression param, IReadOnlyList<Filter> filters)
    {
        Expression? body = null;

        foreach (var filter in filters)
        {
            var filterExpr = BuildSingle(param, filter);
            body = body is null
                ? filterExpr
                : filter.LogicalOperator == LogicalOperator.Or
                    ? Expression.OrElse(body, filterExpr)
                    : Expression.AndAlso(body, filterExpr);
        }

        return body;
    }

    private static Expression BuildSingle(ParameterExpression param, Filter filter)
    {
        var member = Expression.Property(param, filter.PropertyName);

        object value = filter.Value switch
        {
            JsonElement je => ConvertJsonElement(je, member.Type),
            _              => Convert.ChangeType(filter.Value, member.Type)!
        };

        var constant = Expression.Constant(value, member.Type);

        return filter.Operation switch
        {
            Operation.Equals             => Expression.Equal(member, constant),
            Operation.NotEqual           => Expression.NotEqual(member, constant),
            Operation.GreaterThan        => Expression.GreaterThan(member, constant),
            Operation.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, constant),
            Operation.LessThan           => Expression.LessThan(member, constant),
            Operation.LessThanOrEqual    => Expression.LessThanOrEqual(member, constant),
            Operation.Contains           => CallStringMethod(member, constant, ContainsMethod, filter.Operation),
            Operation.StartsWith         => CallStringMethod(member, constant, StartsWithMethod, filter.Operation),
            Operation.EndsWith           => CallStringMethod(member, constant, EndsWithMethod, filter.Operation),
            _ => throw new NotSupportedException($"Unsupported operation {filter.Operation}")
        };
    }

    private static Expression CallStringMethod(MemberExpression member,
                                               ConstantExpression constant,
                                               MethodInfo method,
                                               Operation op)
    {
        if (member.Type != typeof(string))
            throw new NotSupportedException($"{op} can only be used on string properties");

        return Expression.Call(member, method, constant);
    }

    private static object ConvertJsonElement(JsonElement element, Type target)
    {
        if (target == typeof(string)) return element.GetString()!;
        if (target == typeof(int))    return element.GetInt32();
        if (target == typeof(long))   return element.GetInt64();
        if (target == typeof(bool))   return element.GetBoolean();
        if (target.IsEnum)            return Enum.Parse(target, element.GetString()!, true);

        return JsonSerializer.Deserialize(element.GetRawText(), target)!;
    }
}
