using System.Linq.Expressions;
using QuVian.SharedLibrary.Basics.Filters;
using QuVian.SharedLibrary.Basics.Repositories.Interfaces;

namespace QuVian.SharedLibrary.Basics.Repositories.Specifications;

/// <summary>
/// Fluent builder for constructing entity queries passed to the repository.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
[SuppressMessage("Design", "CA1002:Do not expose generic lists")]
public class Specification<TEntity> : ISpecification<TEntity> where TEntity : class, IEntity
{
    private readonly List<Filter> _filters = new();
    private readonly List<FilterGroup> _filterGroups = new();
    private readonly List<Expression<Func<TEntity, object>>> _includeExpressions = new();
    private readonly List<(Expression<Func<TEntity, object>> KeySelector, SortDirection Direction)> _orderExpressions = new();

    public IReadOnlyList<Filter> Filters => _filters;

    public IReadOnlyList<FilterGroup> FilterGroups => _filterGroups;

    public IReadOnlyList<Expression<Func<TEntity, object>>> IncludeExpressions => _includeExpressions;

    public IReadOnlyList<(Expression<Func<TEntity, object>> KeySelector, SortDirection Direction)> OrderExpressions => _orderExpressions;

    public Expression<Func<TEntity, bool>>? WhereExpression { get; private set; }

    public int? Skip { get; private set; }

    public int? Take { get; private set; }

    public Specification<TEntity> AddInclude(Expression<Func<TEntity, object>> includeExpression)
    {
        _includeExpressions.Add(includeExpression);
        return this;
    }

    public Specification<TEntity> AddOrderBy(Expression<Func<TEntity, object>> keySelector)
    {
        _orderExpressions.Add((keySelector, SortDirection.Ascending));
        return this;
    }

    public Specification<TEntity> AddOrderByDescending(Expression<Func<TEntity, object>> keySelector)
    {
        _orderExpressions.Add((keySelector, SortDirection.Descending));
        return this;
    }

    public Specification<TEntity> AddWhere(Expression<Func<TEntity, bool>> whereExpression)
    {
        if (WhereExpression is null)
        {
            WhereExpression = whereExpression;
        }
        else
        {
            var param = WhereExpression.Parameters[0];
            var combined = Expression.AndAlso(
                WhereExpression.Body,
                Expression.Invoke(whereExpression, param));
            WhereExpression = Expression.Lambda<Func<TEntity, bool>>(combined, param);
        }
        return this;
    }

    public Specification<TEntity> AddFilters(IReadOnlyList<Filter> filters)
    {
        _filters.AddRange(filters);
        return this;
    }

    public Specification<TEntity> AddFilterGroup(IReadOnlyList<Filter> filters, LogicalOperator groupOperator = LogicalOperator.And)
    {
        _filterGroups.Add(new FilterGroup { Filters = filters, GroupOperator = groupOperator });
        return this;
    }

    public Specification<TEntity> AddFilterGroup(FilterGroup group)
    {
        _filterGroups.Add(group);
        return this;
    }

    public Specification<TEntity> AddSkip(int skip)
    {
        Skip = skip;
        return this;
    }

    public Specification<TEntity> AddTake(int take)
    {
        Take = take;
        return this;
    }
}
